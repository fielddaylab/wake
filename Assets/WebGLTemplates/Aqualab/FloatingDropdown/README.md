# FloatingDropdown
Simple reusable web component for web games linking to the [Vault](https://vault.fielddaylab.wisc.edu/)

This repository houses a reusable HTML component for the Vault floating dropdown on free browser-based games. There are two pieces used for this component: 
1. The HTML template used to display the component on the dom 
2. the Unity javascript plugin to be used to remove the button after exiting the title screen.

## 1. Adding the Component
This example illustrates how this component can be added to a project

### Step 1: Add a custom component to the header of the index file

```javascript
<script src="FloatingDropdown/index.js"></script>
```

### Step 2: Add the custom element to the body of the file

This element uses ***slots*** to make the component more flexible. If you are unfamiliar with slots, you can read more [here](https://developer.mozilla.org/en-US/docs/Web/API/Web_components/Using_templates_and_slots#adding_flexibility_with_slots)

Example of Template:
```javascript
<body>
    <floating-dropdown>
        <style> /* Add custom fonts here */ </style>
        <img slot="header" src="/path/to/header/image" alt="Vault Games Library" width="250px">
        <span slot="desc" class="content">
            Put the body text here
        </span>
        <span slot="button-label">
            Put button label here
        </span>
    </floating-dropdown>
    <div id="BrainPOPsnapArea">
...

```
