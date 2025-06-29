# Mexican Standoff

A quick mind game, implemented with Blazor and SignalR.

## Components

![image](docs/components.png)


### Deployment

Deploy as a .NET Blazor app, e.g. as an Azure Web App.

Currently deployed at [https://mexican-standoff.azurewebsites.net](https://mexican-standoff.azurewebsites.net).

**azure-webapps-dotnet-core.yml**

The GitHub workflow defined
[`.github/workflows/azure-webapps-dotnet-core.yml`](.github/workflows/azure-webapps-dotnet-core.yml)
will build and deploy the app to Azure.

**NOTE:** In a GitHub repo, set secrets and variables:
- `env.AZURE_WEBAPP_NAME`: Name of the Azure Web App
- `secrets.AZURE_WEBAPP_PUBLISH_PROFILE`: Publish profile of the Azure Web App

## Tailwind

Locally, on Prebuild, we run `npm run build:css`, but to continually build base.css, use

```sh
cd Blazor
npm run watch:css
```
Inspired by:
https://steven-giesel.com/blogPost/364c43d2-b31e-4377-8001-ac75ce78cdc6

---
[Tailwind CLI](https://tailwindcss.com/docs/installation/tailwind-cli)
```sh
npm install tailwindcss @tailwindcss/cli
```

## Color scheme for characters
![image](docs/character-colors.png)
- Red: #E63946
- Off-white: #F1FAEE
- Light Blue: #A8DADC
- Medium Blue: #457B9D
- Dark Blue: #1D3557
- Orange: #F4A261
- Yellow: #E9C46A
- Teal: #2A9D8F

## TODO items

- Support for saving rule-sets in local storage
- Maybe: Bar to go and get life; one at the time? cost a coin?

### Play.razor

- Maybe: support for switching to game state view

### ShowGame.razor
- Remember all winners? Show them when a game ends
- If money was distributed, show it
	- From chest: some animation or at least show a big coin
	- From shooting: some animation or at least show a big coin(s) for each player

## Resources

- SVG icons: https://www.svgrepo.com/
- Sound files: https://freesound.org/
