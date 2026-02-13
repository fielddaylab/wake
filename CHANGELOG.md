## [1.0.3] | 01-20-2026
### Changed
* refactor of game state during logging events
* implementation of new survey set (see [8a70693](https://github.com/fielddaylab/wake/commit/8a70693f7fe0c279c10b541d6e5c996128b8a7ca#diff-e6ebdae8131d28b6b685a8e24b8f43355b2e44b1447c7f532cd637c77a70f403))
* new animations and UI layout for modeling intervention
### Fixed
- extended range of pH values on pH meters
- extended range of temperature values on temperature meters
* microscope camera background color
### Optimization
* rebaked lightmaps for station interiors and helm
* significant render order optimizations to sure skybox textures are rendered at the end of opaque rendering
* ensure all UI canvas elements are set up with ztest disabled
* disabled UI camera depth clear
* improved modeling target discrepancy layout
* cleaned up all scenes to remove need for camera color clear, thereby reducing overdraw
### Added
* full implementation of Spanish localization
* small tweaks to the English script for compatibility with localization system
- fixed missing articles in English
* ensure player cannot open bestiary page without entry
### Removed
- configuration for Firebase app
- job graph research tests 
- removed `Clione` & `ClacialAmphipod` from stress tank