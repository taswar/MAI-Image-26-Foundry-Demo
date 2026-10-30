# Contributing

Thanks for helping improve this sample. It's a .NET 10 console app with five use cases for MAI-Image-2.6 and MAI-Image-2.6-Flash on Microsoft Foundry: a campaign banner, catalog shots, multi-reference editing, personalized cards and image cleanup. It calls the `/mai/v1/images` REST routes with `HttpClient` and Entra ID. Contributions that make it clearer, more correct, or more useful to other .NET developers are welcome.

## Ways to contribute

- **Report a bug.** A use case fails, or the README doesn't match what the code does.
- **Fix the docs.** Typos, unclear steps, or a missing troubleshooting case.
- **Improve the sample.** For example, running only one use case from the command line, or a size option for the edits.
- **Share results.** If you tried other prompts or reference images, an issue describing what worked helps others.

For anything larger than a small fix, open an issue first so we can agree on the approach before you write code.

## Set up

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and the Azure CLI.
2. Clone the repo and build it:

   ```bash
   git clone https://github.com/taswar/MAI-Image-26-Foundry-Demo.git
   cd MAI-Image-26-Foundry-Demo
   dotnet build
   ```

3. To run it, deploy `MAI-Image-2.6` and `MAI-Image-2.6-Flash` on a Microsoft Foundry resource, set `AZURE_AI_FOUNDRY_ENDPOINT` with user secrets, and sign in with `az login`, as described in the [README](README.md#configuration). Your account needs the **Cognitive Services User** role on the resource. A full run makes 15 image calls, so it takes a while and costs money.

## Guidelines

- **Never commit secrets.** No keys, endpoints, tenant IDs or tokens in code, config files or screenshots.
- **Keep the MAI routes.** MAI image models use `/mai/v1/images/generations` and `/mai/v1/images/edits`. Attach each reference image as a form field named `image`, not `image[]`, and send at most 5.
- **Keep Entra ID.** The sample uses `DefaultAzureCredential` with no API key. Keep it that way.
- **Use images you may share.** Reference images and outputs go into a public repo. Only add images you made or have the rights to, and no photos of real people.
- **Keep committed images small.** Keep any image you add under 2 MB, and update the README gallery if you replace an output.
- **Keep the error output.** Failed calls include the status code and response body. Keep that for any new call.
- **Keep it a sample.** The code should stay short and readable in one `Program.cs`. Prefer clarity over abstraction, and add a dependency only when it clearly helps.
- **Match the existing style.** Target `net10.0`, keep nullable reference types on, and follow the layout already in `Program.cs`.
- **Keep the README in sync.** If you add a use case, change settings or change outputs, update the README in the same pull request.
- **One change per pull request.** Small, focused pull requests are easier to review and merge.

## Pull request checklist

- [ ] `dotnet build` succeeds with no new warnings.
- [ ] Any use case you changed runs against your own deployments, or you explain why you couldn't run it.
- [ ] No secrets, endpoints or personal data are in the diff.
- [ ] The README reflects any change in use cases, settings or outputs.
- [ ] The pull request description says what changed and why.

The build workflow runs on every pull request and has to pass before a merge.

## Reporting a bug

Open an issue and include:

- Which use case failed and what you expected.
- The status code and response body from the error, with any endpoints removed.
- Your .NET SDK version (`dotnet --version`) and operating system.
- Your deployment names and deployment type.

## Security issues

Please don't report security problems in a public issue. Use **Security → Report a vulnerability** on the repo to send a private report.

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE) that covers this project.
