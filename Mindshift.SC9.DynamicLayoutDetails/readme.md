# Dynamic Layout Details

Status: Complete and working.

Description: Adds another button in the Ribbon at Presentation -> Layout named "Dynamic". This brings up a dialog very similar to the Details dialog, but it shows all the Renderings in a tree format. This way you can see where each rendering is in the presentation hierarchy. The current version 1.0 is read only until I rewrite the save routing to be compatible with the new XML in Sitecore 9.


Features:
	- Shows renderings that are aren't found in red. If any renderings show in red. This means that the placeholder set on the child renderings weren't found in any of the cshtml files. This will help you to identify any renderings that have an invalid placeholder. Please be aware that this can return a false positive if your placeholder isn't in the chthml file, for eaxample, if it was created through code.
	- If the placeholder is Dynamic it will show a lightning bold next to the name
	- Clicking the Edit icon will show you the current settings.

Upcoming Features:
	- Saving (was in the old version)
	- The ability to move renderings under different placeholders. This will allow you to easily correct mistakes. It will fill in the correct placeholder path automatically when moved (was in the old version)
	- Edit the datasource item (the old version had a very rudimentary button that would open a new browser tab with the item in another  content editor)




This was originally written based on the dynamic placeholder code that came from somewhere else, but it was revamped for Sitecore 9 which has this functionality out of the box. 

The current version 1.0 is read only until I rewrite the save routing to be compatible with the new XML in Sitecore 9.
