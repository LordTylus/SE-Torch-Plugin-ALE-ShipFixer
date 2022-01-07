### Introduction
Every now and then Grids on the server seem to be bugged. So that they wont move any more, cannot be stopped, rotated or accelerated.

This Plugin tries to deal with the situationand offer the Players to fix it themselves without the need of having an Administrator or Moderator involved

### Commands
- !fixship
 - Similar to !fixship &lt;shipname&gt; but it is better with handling naming conflicts as it takes the grid the players is looking at. 
- !fixship &lt;shipname&gt;
 - Stops the grid, cuts and pastes is back. Every player can use this command. But only on grids he has the majority of ownership on.
 - The command wont work of there are active connections to connectors or landing gears, or if there are Players in a cockpit, cryopod or any other block players can sit on. Like for example the toilet. 
- !fixshipmod 
 - Same as !fixship but that it has no check for ownership or cooldowns and can only be performed by moderator and above. 
- !fixshipmod &lt;shipname&gt;
 - Same as !fixship &lt;shipname&gt; but that it has no check for ownership and can only be performed by moderator and above. 

### How it works
When the ship is cut and pasted it can potentially fail if at the very same moment something is blocking the space. So its advised to take a Blueprint first.

PCU Authorship, Block limits and Ownerships will not be affected. After the ship is pasted back in it should behave like before.

### Executing via Console
The console can only run !fixshipmod &lt;shipname&gt;. All the others require the server to either have a character that looks at something or ownership which it cannot have. 

### Configuration
The cooldowns of the commands can be configured.

Default for fixship is 15 Minutes. Default for Confirmation of said command is 30 Seconds. You can find the configuration File in your Instance folder after first launch. Its called ShipFixer.cfg

  &lt;CooldownInSeconds&gt;900&lt;/CooldownInSeconds&gt;
  &lt;ConfirmationInSeconds&gt;30&lt;/ConfirmationInSeconds&gt;

### Github
[https://github.com/LordTylus/SE-Torch-Plugin-ALE-ShipFixer](https://github.com/LordTylus/SE-Torch-Plugin-ALE-ShipFixer)