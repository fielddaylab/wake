mergeInto(LibraryManager.library, {
//   EnableVaultButton: function () {
//     let vaultDropdown = document.querySelector("floating-dropdown")
//     window.alert("Hello, world!");
//   },

  DisableVaultButton: function () {
    var vaultDropdown = document.querySelector('floating-dropdown');
    console.log("Debug [JSPlugin > DisableVaultButton]");
    if (vaultDropdown) { 
      vaultDropdown.remove();
    } else {
      console.warn("[Vault Plugin] Failed attempt to remove element <floating-dropdown>")
    }
  },
});