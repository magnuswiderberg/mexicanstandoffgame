# Mexican Standoff

A quick mind game, implemented with Blazor and SignalR.

## Components

![image](docs/components.png)


### Deployment

To keep the cost low, a good option is to use an Azure Static Web App for deployment.

But as a .NET Blazor app, it can be deployed anywhere you like.

## Tailwind

On Prebuild, we run `npm run build:css`, but to continually build base.css, use

```sh
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

- github actions
- parameterize pipeline yaml file
- support for bots
- example bots
- Support for giving up => QUIT state on Player
- Countdown time

### Play.razor

- Game ended
	- If winner:  Sound. Else other sound
	- If game with same id exists
		- reload the game
		- Offer to rejoin
- Maybe: support for switching to game state view
- After QUIT (to start page), offer rejoin
	- Maybe save played games in local storage? Or in memory?
	- List games in State Created
- Support for removing input name from local storage to get new suggestion
	- Or just support for suggestion
- Maybe keep character id in local storage

### PlayMonitor.razor

- Responsive layout: support for phone
	- Maybe alternative:
		- Use no monitor
		- All players can switch to game state on their device
- Kick player: Add confirm
- Maybe "Fix" button
	- Would try to resolve things
		- Trigger bots again, if there is no selected card
		- etc