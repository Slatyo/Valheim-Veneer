# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0](https://github.com/Slatyo/Valheim-Veneer/compare/v1.0.0...v1.1.0) (2025-12-17)


### Features

* **animation:** add show/hide animator component ([7e2829a](https://github.com/Slatyo/Valheim-Veneer/commit/7e2829ac33a65fe2cbfdf4de182e5334c0158015))
* **api:** add hotbar control and HudRoot property ([af55f4e](https://github.com/Slatyo/Valheim-Veneer/commit/af55f4e673427620d08b3c37d9d8f4f12e494aa4))
* **colors:** add glass effect and glow color palette ([0141d66](https://github.com/Slatyo/Valheim-Veneer/commit/0141d66f6c872018b836d7674dbaf3bdb82e731c))
* **components:** add search input, toggle button, and requirement row ([c1d616c](https://github.com/Slatyo/Valheim-Veneer/commit/c1d616cdbda56ccf37d7426035c65ba44cdc4af5))
* **components:** add VeneerAnimatable base class ([dbd1b0b](https://github.com/Slatyo/Valheim-Veneer/commit/dbd1b0b61be63bace7265dbdb2cfd0b1443cfea2))
* **components:** add VeneerCard and VeneerCardGrid ([f6f14da](https://github.com/Slatyo/Valheim-Veneer/commit/f6f14da25c69aee7f8586d94b25bb61c43e51d65))
* **components:** add VeneerTabBar component ([53001b5](https://github.com/Slatyo/Valheim-Veneer/commit/53001b524eb97a96b8f8ec2cb8dcfd8c70bc02bc))
* **dimensions:** add corner radius constants ([b135a16](https://github.com/Slatyo/Valheim-Veneer/commit/b135a162504216de0ba645d23c7a3f64761e8a32))
* **editmode:** add VeneerMover and VeneerResizer by default to all frames ([adc0067](https://github.com/Slatyo/Valheim-Veneer/commit/adc006731891f94a967af80a0df5def8e20c2672))
* **element:** add animated show/hide transitions ([8eae461](https://github.com/Slatyo/Valheim-Veneer/commit/8eae4615d38daf6a37fa217bfd5ffbf3ce4da084))
* **extensions:** add UI extension system for mod integration ([d841ced](https://github.com/Slatyo/Valheim-Veneer/commit/d841ced3bfff99908f4978d074167b4cefed2c3c))
* **frame:** add glass effect with frost overlay and window tints ([32c08af](https://github.com/Slatyo/Valheim-Veneer/commit/32c08af7d05e67c086e610f88c4eb3f0c9fb9281))
* **grid:** add HideEquippedItems option to VeneerItemGrid ([b6bd5ef](https://github.com/Slatyo/Valheim-Veneer/commit/b6bd5ef45551fb1a95cb9becf30e47ef3b314994))
* **hud:** add layout containers for UI extensions ([a85a734](https://github.com/Slatyo/Valheim-Veneer/commit/a85a7347fadc279b49b821b399a0de122aa01d4e))
* initial release of Veneer UI framework ([48c9d41](https://github.com/Slatyo/Valheim-Veneer/commit/48c9d41ecd1822d5c0df24f9a918581d672c4273))
* **itemslot:** add visual provider system for slot appearance ([000a632](https://github.com/Slatyo/Valheim-Veneer/commit/000a632d51ceab1f40c8d793305da1f70c6defd4))
* **text:** add floating damage text system ([67f4304](https://github.com/Slatyo/Valheim-Veneer/commit/67f43043dd2a7c45a58ef051f51fe776333f2f65))
* **textures:** add circle texture generation ([c6265f6](https://github.com/Slatyo/Valheim-Veneer/commit/c6265f60b5eaa1c04c45a778023e33ef97b7332a))
* **textures:** add rounded rectangle and frost texture generation ([655caf9](https://github.com/Slatyo/Valheim-Veneer/commit/655caf914adc82d1b79bd0c66d04a4b6d57c1cca))
* **theme:** add window tint system and animation utilities ([a1c8ae3](https://github.com/Slatyo/Valheim-Veneer/commit/a1c8ae3c2fb6829cbd117fa6d61a85cd95c60add))
* **tooltip:** add provider system for extensible item tooltips ([7d6c512](https://github.com/Slatyo/Valheim-Veneer/commit/7d6c51208b2f6beb85cb57860b566b81da7c7e7f))


### Bug Fixes

* **ci:** use config and manifest files for release-please ([3ab7427](https://github.com/Slatyo/Valheim-Veneer/commit/3ab74277ee6202bd4b2ceb5761149feb0c7f80e7))
* **inventory:** integrate with vanilla drag system for proper item handling ([0d443f8](https://github.com/Slatyo/Valheim-Veneer/commit/0d443f828ce23dc39ea82f0356fc1f8adeb1b08b))
* **primitives:** improve button disabled state and text initialization ([7496a1f](https://github.com/Slatyo/Valheim-Veneer/commit/7496a1fcc915a7bd4ee20be6d6ef20d2eb9ea103))
* **quickbar:** improve PvP toggle synchronization ([d70202d](https://github.com/Slatyo/Valheim-Veneer/commit/d70202d66136380f3f497690bb58c5c750e4cfb8))
* tooltip and UI improvements ([44542a5](https://github.com/Slatyo/Valheim-Veneer/commit/44542a5b9c4a0296937bc75a060f70aa28bacfd9))
* **tooltip:** add background, border and smart corner positioning ([56a414a](https://github.com/Slatyo/Valheim-Veneer/commit/56a414a4106c48c0b5d5cb6389fe7b78f6ea5b40))
* **tooltip:** reinitialize on scene transitions ([9ed7b6d](https://github.com/Slatyo/Valheim-Veneer/commit/9ed7b6d30e0fd62900b504bb714cc346f0414d3a))


### Code Refactoring

* **compendium:** use VeneerTabBar for category filtering ([b0b0b27](https://github.com/Slatyo/Valheim-Veneer/commit/b0b0b2743154f0ee46225fa9389f54ed51e5571f))
* consolidate VeneerWindow into VeneerFrame and fix panel headers ([553d99a](https://github.com/Slatyo/Valheim-Veneer/commit/553d99a009d7a9b6624c3e794f8cc2d67b3a4ac6))
* **crafting:** improve panel layout and component usage ([82c1c0a](https://github.com/Slatyo/Valheim-Veneer/commit/82c1c0a21e5e645143572c083fd008fb1df15a7a))
* **frame:** remove obsolete Moveable property from FrameConfig ([4efa874](https://github.com/Slatyo/Valheim-Veneer/commit/4efa874b182ecc5de400dfa3a4f9223e96014d70))
* **inventory:** remove equipment slots from inventory panel ([43e0b8f](https://github.com/Slatyo/Valheim-Veneer/commit/43e0b8fec7bb1b3b873fa345b18df781341dca41))
* **primitives:** use rounded corners for all components ([2264439](https://github.com/Slatyo/Valheim-Veneer/commit/2264439303a668cc81e8d7c6385dd5329f33b1e2))
* **vanilla:** apply window tints and glass effect to all panels ([93f1a1e](https://github.com/Slatyo/Valheim-Veneer/commit/93f1a1e85cbf90a9fd89000422b5ee86c37cf7a4))

## [1.0.0] - 2025-12-01

### Added
- VeneerPanel component with Wood, Stone, Dark, and Minimal styles
- VeneerButton component with Primary, Secondary, Danger, and Ghost styles
- VeneerText component with Header, Subheader, Body, Caption, Value, and Italic styles
- VeneerTooltip system with rich formatting and item tooltips
- VeneerNotify system with toast notifications and alerts
- VeneerColors palette with rarity colors and feedback colors
- VeneerTheme for global styling configuration
- VeneerInput for input blocking and escape key handling
- VeneerCursor for cursor state management
- Keybind registration for panel toggles
