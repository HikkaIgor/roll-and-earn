import * as anchor from "@coral-xyz/anchor";

module.exports = async function (provider: anchor.Provider) {
  anchor.setProvider(provider);
};
