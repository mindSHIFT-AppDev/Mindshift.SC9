
# Mindshift.SC9
Sitecore modules targeted for Sitecore 9+

Some modules are incomplete or even just ideas. I will lay that out below.



## Projects
Most modules contain the following project types. In the new SC9 version I've moved slightly towards a Helix approach.
1. TDS.{Database} Projects - these contain the Sitecore items needed for the module. They are separated by the Sitecore database.
2. .NET Projects - This contains all the web files and code that are needed for the module. 
3. TDS.Build Projects - this is a TDS project that just handles the package creation for a module. We have taken to making this a separate project.

## Modules
This repository contains a number of different modules. Here is a short description of each of them. Please see the README.md under each project for more information.

### Common
These projects contain items, classes and files that are shared between all modules. Including the following:
1. An VueJS framework for creating dialogs.
2. A Generic Web API route for all modules.
3. Base classes for Web API Controllers, Mappings, Pipelines.
4. Custom Field Types, Enumerations, Dialogs.

### Dynamic Placeholders
Status: Complete and working.

Description: Adds another button in the Ribbon at Presentation -> Layout named "Dynamic". This brings up a dialog very similar to the Details dialog, but it shows all the Renderings in a tree format. This way you can see where each rendering is in the presentation hierarchy. The current version 1.0 is read only until I rewrite the save routing to be compatible with the new XML in Sitecore 9.


### Datasource Content Tab
Status: In progress

Description: Adds a new tab to the Content Editors Editor Pane that will list all the Datasources for the Renderings on the page and allow you to edit them directly in that tab. This effectively gives you a view of the modular content in the Content Editor itself.


## Future Modules

### AutoPublish
Status: This is working in the old version but needs to be ported over. Will do this soon.

Description: This module gives you the ability to schedule publish times via configuration items under System/Modules.
