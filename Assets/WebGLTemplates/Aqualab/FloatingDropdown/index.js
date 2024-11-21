// This is where the custom component is defined
fetch('FloatingDropdown/index.html')
    .then(stream => stream.text())
    .then(text => define(text))

/**
 * Sets up the custom component as defined in the FloatingDropdown template
 * 
 * @param {string} html the outerHTML attribute of the floating dropdown element defined in `index.html` 
 */
function define(html) {
    class FloatingDropdown extends HTMLElement {
        constructor() {
            self = super();
        }

        connectedCallback() {
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, "text/html");

            const template = doc.getElementById("floating-dropdown");
            //Custom element
            const dropdown = template.content.cloneNode(true);

            const collapsible = dropdown.querySelector(' #collapsible ');
            collapsible.addEventListener("mouseenter", () => toggleCollapsibleContent(collapsible));
            collapsible.addEventListener("mouseleave", () => toggleCollapsibleContent(collapsible));

            const shadowRoot = this.attachShadow({mode: "open"})
            shadowRoot.appendChild(dropdown);

            // Apply external styles to the shadow dom
            const styles = document.createElement("link");
            styles.setAttribute("rel", "stylesheet");
            styles.setAttribute("href", "FloatingDropdown/styles.css");

            shadowRoot.appendChild(styles);
        }
  }
  customElements.define("floating-dropdown", FloatingDropdown);
}

/**
 * Handles the collapse functionality for .content elements in a collapsible
 * 
 * @param {*} element the root collapsible element containing content to hide
 */
function toggleCollapsibleContent(element) { 
    console.log("DEBUG [toggleCollapsible]");
    let contentList = element.querySelectorAll('.content');

    let i;

    for (i = 0; i < contentList.length; i++){
        if (contentList[i].style.display === "flex") {
            contentList[i].style.display = "none";
        } else {
            contentList[i].style.display = "flex";
        }
    }
}