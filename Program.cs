using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var endpoint = config["AZURE_AI_FOUNDRY_ENDPOINT"]
    ?? throw new InvalidOperationException(
        "AZURE_AI_FOUNDRY_ENDPOINT is not set. Run: dotnet user-secrets set \"AZURE_AI_FOUNDRY_ENDPOINT\" \"<your-endpoint>\"");

var credential = new DefaultAzureCredential();
AccessToken token = await credential.GetTokenAsync(
    new TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" }));

using var http = new HttpClient();
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

async Task<byte[]> GenerateImageAsync(string model, string prompt, int width = 1024, int height = 1024)
{
    string url = $"{endpoint.TrimEnd('/')}/mai/v1/images/generations?api-version=preview";

    var payload = new
    {
        prompt,
        model,
        width,
        height,
        output_compression = 100,
        output_format = "png",
        n = 1
    };

    using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    using HttpResponseMessage response = await http.PostAsync(url, content);
    string body = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException($"{model} generation failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{body}");
    }

    using JsonDocument json = JsonDocument.Parse(body);
    string b64 = json.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString()
        ?? throw new InvalidOperationException("Response missing data[0].b64_json.");

    return Convert.FromBase64String(b64);
}

//Use Case 1: Marketing & Campaign Production — Legible Text, First Try
var campaignPrompt = """
    A wide-format e-commerce banner ad for a summer sale. Clean minimalist studio
    background in soft pastel blue. A pair of white running shoes floating at a
    slight angle, dramatic soft shadow beneath. Bold, perfectly legible headline
    text at the top reading "SUMMER SALE — 30% OFF" in a clean modern sans-serif
    font, white text with subtle drop shadow. Small subtext below reading
    "Limited time only" in a lighter weight. Professional advertising photography
    style, high detail, 1.5K resolution.
    """;

byte[] campaignAsset = await GenerateImageAsync("MAI-Image-2.6", campaignPrompt, width: 1536, height: 864);
await File.WriteAllBytesAsync("campaign-summer-sale-banner.png", campaignAsset);
Console.WriteLine("Saved: campaign-summer-sale-banner.png");

//Use Case 2: Retail & E-Commerce Catalogs — Consistent Imagery at Volume
var catalogItems = new[]
{
    new { Sku = "SKU-1001", Description = "a matte black ceramic coffee mug" },
    new { Sku = "SKU-1002", Description = "a glossy red ceramic coffee mug" },
    new { Sku = "SKU-1003", Description = "a sage green ceramic coffee mug" },
    new { Sku = "SKU-1004", Description = "a cream white ceramic coffee mug" },
};
 
const string catalogStyleTemplate = """
    A studio product photo of {0} on a plain white seamless background,
    soft even lighting from the upper left, subtle reflection beneath,
    centered composition, e-commerce catalog style, no props, no text.
    """;

foreach (var item in catalogItems)
{
    var prompt = string.Format(catalogStyleTemplate, item.Description);
    byte[] image = await GenerateImageAsync("MAI-Image-2.6-Flash", prompt);
    string outPath = $"catalog-{item.Sku}.png";
    await File.WriteAllBytesAsync(outPath, image);
    Console.WriteLine($"Saved: {outPath}");
}

//Use Case 3: Brand & Creative Operations — Multi-Reference Editing

async Task<byte[]> EditWithReferencesAsync(string model, string editPrompt, params string[] referenceImagePaths)
{
    if (referenceImagePaths.Length == 0 || referenceImagePaths.Length > 5)
    {
        throw new ArgumentException("Provide between 1 and 5 reference images.");
    }

    string url = $"{endpoint.TrimEnd('/')}/mai/v1/images/edits?api-version=preview";

    using var content = new MultipartFormDataContent
    {
        { new StringContent(editPrompt), "prompt" },
        { new StringContent(model), "model" }
    };

    foreach (var path in referenceImagePaths)
    {
        byte[] bytes = await File.ReadAllBytesAsync(path);
        var imagePart = new ByteArrayContent(bytes);
        imagePart.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(imagePart, "image", Path.GetFileName(path));
    }

    using HttpResponseMessage response = await http.PostAsync(url, content);
    string body = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException($"{model} multi-reference edit failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{body}");
    }

    using JsonDocument json = JsonDocument.Parse(body);
    string b64 = json.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString()
        ?? throw new InvalidOperationException("Response missing data[0].b64_json.");

    return Convert.FromBase64String(b64);
}
 
// Lock a brand mascot + product shape as references, then reformat for three channels.
var channelFormats = new[]
{
    ("instagram-square.png", "Recompose for an Instagram square post (1:1), mascot on the right, product centered-left."),
    ("story-vertical.png", "Recompose for a 9:16 vertical story format, mascot at the bottom, product taking up the upper two-thirds."),
    ("banner-wide.png", "Recompose for a wide horizontal banner (16:9), mascot and product side by side with even spacing."),
};

foreach (var (fileName, formatPrompt) in channelFormats)
{
    byte[] reformatted = await EditWithReferencesAsync(
        "MAI-Image-2.6",
        formatPrompt + " Keep the mascot design, product shape, and brand colors identical to the references.",
        "brand-mascot-reference.png",
        "product-shape-reference.png");

    await File.WriteAllBytesAsync(fileName, reformatted);
    Console.WriteLine($"Saved: {fileName}");
}

// Case 4 Personalization at Scale — Flash in an Interactive Loop

async Task<byte[]> GeneratePersonalizedCardAsync(string recipientName, string occasion, string themeColor)
{
    var prompt = $"""
        A festive greeting card illustration for a {occasion} celebration.
        Hand-lettered style text reading "Happy {occasion}, {recipientName}!"
        centered at the top. Warm {themeColor} color palette, soft decorative
        flourishes around the border, friendly and celebratory illustration style,
        square format.
        """;
 
    return await GenerateImageAsync("MAI-Image-2.6-Flash", prompt, width: 1024, height: 1024);
}
 
// Simulates generating cards for a batch of users in an interactive path —
// fast enough per-call that this could sit behind a live API endpoint.
var requests = new[]
{
    ("Priya", "Birthday", "coral and gold"),
    ("Marcus", "Work Anniversary", "navy and silver"),
    ("Aiko", "Graduation", "sage green and cream"),
};

foreach (var (name, occasion, colors) in requests)
{
    var started = DateTime.UtcNow;
    byte[] card = await GeneratePersonalizedCardAsync(name, occasion, colors);
    var elapsed = DateTime.UtcNow - started;

    string fileName = $"card-{name.ToLowerInvariant()}.png";
    await File.WriteAllBytesAsync(fileName, card);
    Console.WriteLine($"Saved: {fileName} ({elapsed.TotalSeconds:F1}s)");
}

// Case 5: Image Cleanup & Surgical Editing
var cleanupPrompt = """
    Remove the motion blur from the product in this image and sharpen the focus.
    Replace the scuffed cardboard background with a clean plain white studio
    background. Keep the product's position, angle, and proportions exactly
    the same — do not regenerate or redesign the product itself.
    """;
 
byte[] cleanedUp = await EditWithReferencesAsync(
    "MAI-Image-2.6",
    cleanupPrompt,
    "warehouse-photo-raw.png");
 
await File.WriteAllBytesAsync("product-cleaned.png", cleanedUp);
Console.WriteLine("Saved: product-cleaned.png");