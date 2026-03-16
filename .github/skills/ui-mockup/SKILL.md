---
name: ui-mockup
description: Guidelines and best practices for authoring UI mockups.
---

- Create UI mockups only using plain HTML, CSS, and if needed, a little JavaScript. Each UI mockup should include:
  - an `index.html` file that contains the HTML structure.
  - CSS styles should either be inline (in index.html) or in a separate `styles.css` file.
  - If needed, a separate `script.js` file should contain any necessary JavaScript.

- The idea is to create simple, static mockups that can be easily viewed in a web browser. These mockups should focus on the layout and design of the UI components rather than complex functionality.

- Using F12 developer tools in the browser, one should be able to inspect various aspects of the UI elements/controls: their dimensions, colors, fonts, box model, other CSS properties.

- MudBlazor controls should be used as a reference for the design and layout of the UI components, but the mockups should be implemented using plain HTML and CSS.
