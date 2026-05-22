using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using Solana.Unity.SDK;
using UnityEngine;
using Newtonsoft.Json;

namespace RollAndEarn
{
    public class AnchorClient : MonoBehaviour
    {
        private GameConfig gameConfig;

        private GameConfig GetConfig() => gameConfig ??= GameConfigProvider.Instance != null ? GameConfigProvider.Instance.Config : null;

        private PublicKey ProgramId => new(GetConfig().programId);

        private static readonly PublicKey ComputeBudgetProgramId = new("ComputeBudget111111111111111111111111111111");
        private static readonly PublicKey MetaplexTokenMetadataProgramId = new("metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt918Cms");
        private static readonly PublicKey RentSysvar = new("SysvarRent111111111111111111111111111111111");
        private static readonly PublicKey ClockSysvar = new("SysvarC1ock11111111111111111111111111111111");

        public PublicKey DeriveGamePda()
        {
            PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("game_state") }, ProgramId, out var pda, out _);
            return pda;
        }

        public PublicKey DeriveTreasuryPda()
        {
            PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("treasury") }, ProgramId, out var pda, out _);
            return pda;
        }

        public PublicKey DeriveMintAuthorityPda()
        {
            PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("mint_authority") }, ProgramId, out var pda, out _);
            return pda;
        }

        public PublicKey DerivePlayerProfilePda(PublicKey owner)
        {
            PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("player_profile"), owner.KeyBytes }, ProgramId, out var pda, out _);
            return pda;
        }

        public PublicKey DeriveCharacterMintPda(PublicKey owner)
        {
            PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("character_mint"), owner.KeyBytes }, ProgramId, out var pda, out _);
            return pda;
        }

        public PublicKey DeriveItemMintPda(PublicKey owner, byte itemsMinted)
        {
            PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("item_mint"), owner.KeyBytes, new[] { itemsMinted } }, ProgramId, out var pda, out _);
            return pda;
        }

        public PublicKey DeriveMetadataPda(PublicKey mint)
        {
            PublicKey.TryFindProgramAddress(
                new[] { Encoding.UTF8.GetBytes("metadata"), MetaplexTokenMetadataProgramId.KeyBytes, mint.KeyBytes },
                MetaplexTokenMetadataProgramId, out var pda, out _);
            return pda;
        }

        public TransactionInstruction BuildInitGameInstruction(PublicKey authority, PublicKey rewardMint)
        {
            var gamePda = DeriveGamePda();
            var mintAuthPda = DeriveMintAuthorityPda();

            return new TransactionInstruction
            {
                ProgramId = ProgramId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.Writable(authority, true),
                    AccountMeta.Writable(gamePda, false),
                    AccountMeta.Writable(mintAuthPda, false),
                    AccountMeta.ReadOnly(rewardMint, false),
                    AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                    AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
                },
                Data = ComputeDiscriminator("init_game"),
            };
        }

        public TransactionInstruction BuildInitTreasuryInstruction(PublicKey authority, PublicKey rewardMint)
        {
            var gamePda = DeriveGamePda();
            var treasuryPda = DeriveTreasuryPda();
            var treasuryAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(treasuryPda, rewardMint);

            return new TransactionInstruction
            {
                ProgramId = ProgramId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.Writable(authority, true),
                    AccountMeta.Writable(gamePda, false),
                    AccountMeta.Writable(treasuryPda, false),
                    AccountMeta.Writable(treasuryAta, false),
                    AccountMeta.ReadOnly(rewardMint, false),
                    AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                    AccountMeta.ReadOnly(AssociatedTokenAccountProgram.ProgramIdKey, false),
                    AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
                },
                Data = ComputeDiscriminator("init_treasury"),
            };
        }

        public TransactionInstruction BuildCreateCharacterInstruction(
            PublicKey player, string name, string symbol, string uri, byte classType)
        {
            var playerProfilePda = DerivePlayerProfilePda(player);
            var characterMintPda = DeriveCharacterMintPda(player);
            var mintAuthPda = DeriveMintAuthorityPda();
            var characterAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(player, characterMintPda);
            var gamePda = DeriveGamePda();

            var data = new List<byte>();
            data.AddRange(ComputeDiscriminator("create_character"));
            data.AddRange(BorshSerializeString(name));
            data.AddRange(BorshSerializeString(symbol));
            data.AddRange(BorshSerializeString(uri));
            data.Add(classType);

            return new TransactionInstruction
            {
                ProgramId = ProgramId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.Writable(player, true),
                    AccountMeta.Writable(playerProfilePda, false),
                    AccountMeta.Writable(characterMintPda, false),
                    AccountMeta.Writable(characterAta, false),
                    AccountMeta.ReadOnly(mintAuthPda, false),
                    AccountMeta.ReadOnly(gamePda, false),
                    AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                    AccountMeta.ReadOnly(AssociatedTokenAccountProgram.ProgramIdKey, false),
                    AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
                },
                Data = data.ToArray(),
            };
        }

        public TransactionInstruction BuildRollActionInstruction(
            PublicKey player, PublicKey rewardMint, byte adventureType)
        {
            var playerProfilePda = DerivePlayerProfilePda(player);
            var gamePda = DeriveGamePda();
            var treasuryPda = DeriveTreasuryPda();
            var treasuryAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(treasuryPda, rewardMint);
            var playerAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(player, rewardMint);

            var data = new List<byte>();
            data.AddRange(ComputeDiscriminator("roll_action"));
            data.Add(adventureType);

            return new TransactionInstruction
            {
                ProgramId = ProgramId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.Writable(player, true),
                    AccountMeta.Writable(playerProfilePda, false),
                    AccountMeta.Writable(gamePda, false),
                    AccountMeta.ReadOnly(treasuryPda, false),
                    AccountMeta.Writable(treasuryAta, false),
                    AccountMeta.Writable(playerAta, false),
                    AccountMeta.ReadOnly(rewardMint, false),
                    AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                    AccountMeta.ReadOnly(ClockSysvar, false),
                },
                Data = data.ToArray(),
            };
        }

        public TransactionInstruction BuildClaimItemInstruction(
            PublicKey player, string name, string symbol, string uri, byte itemsMinted)
        {
            var playerProfilePda = DerivePlayerProfilePda(player);
            var itemMintPda = DeriveItemMintPda(player, itemsMinted);
            var mintAuthPda = DeriveMintAuthorityPda();
            var itemAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(player, itemMintPda);
            var gamePda = DeriveGamePda();

            var data = new List<byte>();
            data.AddRange(ComputeDiscriminator("claim_item"));
            data.AddRange(BorshSerializeString(name));
            data.AddRange(BorshSerializeString(symbol));
            data.AddRange(BorshSerializeString(uri));

            return new TransactionInstruction
            {
                ProgramId = ProgramId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.Writable(player, true),
                    AccountMeta.Writable(playerProfilePda, false),
                    AccountMeta.Writable(itemMintPda, false),
                    AccountMeta.Writable(itemAta, false),
                    AccountMeta.ReadOnly(mintAuthPda, false),
                    AccountMeta.ReadOnly(gamePda, false),
                    AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                    AccountMeta.ReadOnly(AssociatedTokenAccountProgram.ProgramIdKey, false),
                    AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
                },
                Data = data.ToArray(),
            };
        }

        public TransactionInstruction BuildEquipItemInstruction(
            PublicKey player, PublicKey itemMint, PublicKey itemTokenAccount, byte itemType)
        {
            var playerProfilePda = DerivePlayerProfilePda(player);
            var gamePda = DeriveGamePda();

            var data = new List<byte>();
            data.AddRange(ComputeDiscriminator("equip_item"));
            data.Add(itemType);

            return new TransactionInstruction
            {
                ProgramId = ProgramId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.ReadOnly(player, true),
                    AccountMeta.Writable(playerProfilePda, false),
                    AccountMeta.ReadOnly(itemMint, false),
                    AccountMeta.ReadOnly(itemTokenAccount, false),
                    AccountMeta.ReadOnly(gamePda, false),
                },
                Data = data.ToArray(),
            };
        }

        public TransactionInstruction BuildUnequipItemInstruction(PublicKey player, byte itemType)
        {
            var playerProfilePda = DerivePlayerProfilePda(player);
            var gamePda = DeriveGamePda();

            var data = new List<byte>();
            data.AddRange(ComputeDiscriminator("unequip_item"));
            data.Add(itemType);

            return new TransactionInstruction
            {
                ProgramId = ProgramId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.ReadOnly(player, true),
                    AccountMeta.Writable(playerProfilePda, false),
                    AccountMeta.ReadOnly(gamePda, false),
                },
                Data = data.ToArray(),
            };
        }

        public TransactionInstruction BuildRequestAirdropInstruction(PublicKey player, PublicKey rewardMint)
        {
            var playerProfilePda = DerivePlayerProfilePda(player);
            var treasuryPda = DeriveTreasuryPda();
            var treasuryAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(treasuryPda, rewardMint);
            var playerAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(player, rewardMint);

            return new TransactionInstruction
            {
                ProgramId = ProgramId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.Writable(player, true),
                    AccountMeta.Writable(playerProfilePda, false),
                    AccountMeta.ReadOnly(treasuryPda, false),
                    AccountMeta.Writable(treasuryAta, false),
                    AccountMeta.Writable(playerAta, false),
                    AccountMeta.ReadOnly(rewardMint, false),
                    AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                },
                Data = ComputeDiscriminator("request_airdrop"),
            };
        }

        public TransactionInstruction BuildLevelUpInstruction(
            PublicKey player, PublicKey rewardMint)
        {
            var playerProfilePda = DerivePlayerProfilePda(player);
            var gamePda = DeriveGamePda();
            var playerAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(player, rewardMint);

            return new TransactionInstruction
            {
                ProgramId = ProgramId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.Writable(player, true),
                    AccountMeta.Writable(playerProfilePda, false),
                    AccountMeta.ReadOnly(gamePda, false),
                    AccountMeta.Writable(playerAta, false),
                    AccountMeta.Writable(rewardMint, false),
                    AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                },
                Data = ComputeDiscriminator("level_up"),
            };
        }

        public TransactionInstruction BuildForceCloseProfileInstruction(PublicKey player, PublicKey[] extraAccounts = null)
        {
            var playerProfilePda = DerivePlayerProfilePda(player);

            var keys = new List<AccountMeta>
            {
                AccountMeta.Writable(player, true),
                AccountMeta.Writable(playerProfilePda, false),
            };

            if (extraAccounts != null)
            {
                foreach (var acc in extraAccounts)
                    keys.Add(AccountMeta.Writable(acc, false));
            }

            return new TransactionInstruction
            {
                ProgramId = ProgramId,
                Keys = keys,
                Data = ComputeDiscriminator("force_close_profile"),
            };
        }

        public TransactionInstruction BuildInitProfileInstruction(PublicKey player)
        {
            var playerProfilePda = DerivePlayerProfilePda(player);

            return new TransactionInstruction
            {
                ProgramId = ProgramId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.Writable(player, true),
                    AccountMeta.Writable(playerProfilePda, false),
                    AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
                },
                Data = ComputeDiscriminator("init_profile"),
            };
        }

        public TransactionInstruction BuildRecreateProfileInstruction(PublicKey player, byte classType)
        {
            var playerProfilePda = DerivePlayerProfilePda(player);
            var characterMintPda = DeriveCharacterMintPda(player);

            var data = new List<byte>();
            data.AddRange(ComputeDiscriminator("recreate_profile"));
            data.Add(classType);

            return new TransactionInstruction
            {
                ProgramId = ProgramId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.Writable(player, true),
                    AccountMeta.Writable(playerProfilePda, false),
                    AccountMeta.ReadOnly(characterMintPda, false),
                },
                Data = data.ToArray(),
            };
        }

        public TransactionInstruction BuildClaimDailyRewardInstruction(
            PublicKey player, PublicKey rewardMint)
        {
            var playerProfilePda = DerivePlayerProfilePda(player);
            var treasuryPda = DeriveTreasuryPda();
            var treasuryAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(treasuryPda, rewardMint);
            var playerAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(player, rewardMint);

            return new TransactionInstruction
            {
                ProgramId = ProgramId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.Writable(player, true),
                    AccountMeta.Writable(playerProfilePda, false),
                    AccountMeta.ReadOnly(treasuryPda, false),
                    AccountMeta.Writable(treasuryAta, false),
                    AccountMeta.Writable(playerAta, false),
                    AccountMeta.ReadOnly(rewardMint, false),
                    AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                    AccountMeta.ReadOnly(ClockSysvar, false),
                },
                Data = ComputeDiscriminator("claim_daily_reward"),
            };
        }

        public async UniTask<string> SendTransactionAsync(TransactionInstruction[] instructions)
        {
            Debug.Log($"[AnchorClient] SendTransactionAsync with {instructions.Length} instructions");
            var wallet = Web3.Wallet;
            if (wallet == null) throw new Exception("Web3.Wallet is null — wallet not connected");

            string blockHash = await wallet.GetBlockHash();
            if (string.IsNullOrEmpty(blockHash))
            {
                Debug.LogWarning("[AnchorClient] Wallet GetBlockHash returned empty, trying direct RPC...");
                var directRpc = Web3.Rpc;
                var bhResult = await directRpc.GetLatestBlockHashAsync(Commitment.Processed);
                blockHash = bhResult?.Result?.Value?.Blockhash;
                Debug.Log($"[AnchorClient] Direct RPC blockhash: {blockHash}");
            }
            if (string.IsNullOrEmpty(blockHash))
            {
                var bhResult2 = await Web3.Rpc.GetLatestBlockHashAsync(Commitment.Finalized);
                blockHash = bhResult2?.Result?.Value?.Blockhash;
                Debug.Log($"[AnchorClient] Finalized blockhash: {blockHash}");
            }
            if (string.IsNullOrEmpty(blockHash))
                throw new Exception("Failed to get blockhash from local validator. Ensure validator is running.");
            Debug.Log($"[AnchorClient] Blockhash: {blockHash}");

            var allInstructions = new List<TransactionInstruction>
            {
                BuildSetComputeUnitPrice(140_000),
                BuildSetComputeUnitLimit(400_000),
            };
            allInstructions.AddRange(instructions);

            var tx = new Transaction
            {
                FeePayer = wallet.Account.PublicKey,
                RecentBlockHash = blockHash,
                Instructions = allInstructions,
            };

            Debug.Log($"[AnchorClient] FeePayer: {wallet.Account.PublicKey}, instructions: {allInstructions.Count}");

            string txSignature = null;
            try
            {
                var result = await wallet.SignAndSendTransaction(tx, skipPreflight: true, commitment: Commitment.Processed);
                if (result != null && result.WasSuccessful && !string.IsNullOrEmpty(result.Result))
                {
                    txSignature = result.Result;
                    Debug.Log($"[AnchorClient] Tx accepted by RPC: {txSignature}");
                }
                else if (result != null && !result.WasSuccessful)
                {
                    var errData = result.ErrorData;
                    string errType = errData?.Error?.Type.ToString() ?? "unknown";
                    string errMsg = errType;
                    if (errData?.Logs != null && errData.Logs.Length > 0)
                    {
                        foreach (var log in errData.Logs)
                            Debug.LogError($"[AnchorClient] LOG: {log}");
                        errMsg += " | " + errData.Logs[errData.Logs.Length - 1];
                    }
                    Debug.LogError($"[AnchorClient] Tx rejected: {errMsg}");
                    Debug.LogError($"[AnchorClient] Raw response: {result.RawRpcResponse}");
                    throw new Exception($"Transaction rejected: {errMsg}");
                }
            }
            catch (Exception sdkEx) when (sdkEx.Message.Contains("same key"))
            {
                Debug.LogWarning($"[AnchorClient] SignAndSendTransaction threw (SDK bug): {sdkEx.Message}");
            }

            if (string.IsNullOrEmpty(txSignature) && tx.Signatures.Count > 0)
                txSignature = Convert.ToBase64String(tx.Signatures[0].Signature);

            if (string.IsNullOrEmpty(txSignature))
                throw new Exception("Failed to send transaction — no signature obtained");

            string cluster = "devnet";
            var cfgProv = GameConfigProvider.Instance;
            if (cfgProv?.Config != null)
            {
                string rpcEndpoint = cfgProv.Config.rpcEndpoint ?? "";
                if (rpcEndpoint.Contains("localhost") || rpcEndpoint.Contains("127.0.0.1"))
                    cluster = "custom&customUrl=http://localhost:8899";
            }
            Debug.Log($"[AnchorClient] Explorer: https://explorer.solana.com/tx/{txSignature}?cluster={cluster}");

            var rpc = Web3.Rpc;
            for (int i = 0; i < 15; i++)
            {
                await UniTask.Delay(2000);
                var sigList = new List<string> { txSignature };
                var status = await rpc.GetSignatureStatusesAsync(sigList);
                var s = status?.Result?.Value?[0];
                if (s == null) { Debug.Log($"[AnchorClient] Confirming... ({i + 1})"); continue; }
                string confStatus = s.ConfirmationStatus;
                if (confStatus == "confirmed" || confStatus == "finalized" || confStatus == "processed")
                {
                if (s.Error != null)
                {
                    string rawRpc = status.RawRpcResponse ?? "";
                    Debug.Log($"[AnchorClient] Raw error response: {rawRpc}");
                    string errDetail = ParseTransactionError(s.Error, rawRpc);
                    throw new Exception($"Tx failed on-chain: {errDetail}");
                }
                    Debug.Log($"[AnchorClient] Tx confirmed: {txSignature}");
                    return txSignature;
                }
                Debug.Log($"[AnchorClient] Status: {confStatus}, err={s.Error}");
            }
            Debug.LogWarning("[AnchorClient] Tx not confirmed after 30s, returning sig anyway");
            return txSignature;
        }

        private static readonly Dictionary<int, string> AnchorErrorMessages = new()
        {
            { 6000, "Invalid character class" },
            { 6001, "Name too long" },
            { 6002, "Symbol too long" },
            { 6003, "Invalid adventure type" },
            { 6004, "Cooldown active — wait for cooldown to expire" },
            { 6005, "Not eligible for item" },
            { 6006, "Invalid item type" },
            { 6007, "Insufficient XP" },
            { 6008, "Daily reward cooldown active" },
            { 6009, "Invalid metadata PDA" },
            { 6010, "Airdrop already claimed" },
        };

        private static string ParseTransactionError(Solana.Unity.Rpc.Models.TransactionError error, string rawRpc = "")
        {
            string raw = error.Type.ToString();
            try
            {
                if (!string.IsNullOrEmpty(rawRpc))
                {
                    var rawMatch = System.Text.RegularExpressions.Regex.Match(rawRpc, @"""Custom"":(\d+)");
                    if (rawMatch.Success && int.TryParse(rawMatch.Groups[1].Value, out int rawCode))
                    {
                        if (AnchorErrorMessages.TryGetValue(rawCode, out string rawMsg))
                            return $"{rawMsg} (error {rawCode})";
                        return $"Custom error {rawCode}";
                    }
                }
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(error);
                Debug.Log($"[AnchorClient] Full error JSON: {json}");
                var match = System.Text.RegularExpressions.Regex.Match(json, @"""Custom"":(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int code))
                {
                    if (AnchorErrorMessages.TryGetValue(code, out string msg))
                        return $"{msg} (error {code})";
                    return $"Custom error {code}";
                }
                var instrMatch = System.Text.RegularExpressions.Regex.Match(json, @"""InstructionError"":\[\d+,\s*""?(\w+)""?");
                if (instrMatch.Success)
                    return $"{raw}: {instrMatch.Groups[1].Value}";
            }
            catch { }
            return raw;
        }

        private static byte[] ComputeDiscriminator(string instructionName)
        {
            var preimage = $"global:{instructionName}";
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(preimage));
            var discriminator = new byte[8];
            Array.Copy(hash, discriminator, 8);
            return discriminator;
        }

        private static byte[] BorshSerializeString(string value)
        {
            var utf8 = Encoding.UTF8.GetBytes(value);
            var result = new byte[4 + utf8.Length];
            var len = BitConverter.GetBytes((uint)utf8.Length);
            Array.Copy(len, result, 4);
            Array.Copy(utf8, 0, result, 4, utf8.Length);
            return result;
        }

        private static byte[] BorshSerializeU64(ulong value)
        {
            var bytes = new byte[8];
            for (int i = 0; i < 8; i++) bytes[i] = (byte)(value >> (i * 8));
            return bytes;
        }

        private static TransactionInstruction BuildSetComputeUnitPrice(ulong microLamports)
        {
            var data = new byte[9];
            data[0] = 3;
            var val = BitConverter.GetBytes(microLamports);
            Array.Copy(val, 0, data, 1, 8);
            return new TransactionInstruction
            {
                ProgramId = ComputeBudgetProgramId,
                Keys = new List<AccountMeta>(),
                Data = data,
            };
        }

        private static TransactionInstruction BuildSetComputeUnitLimit(ulong units)
        {
            var data = new byte[5];
            data[0] = 2;
            var val = BitConverter.GetBytes((uint)units);
            Array.Copy(val, 0, data, 1, 4);
            return new TransactionInstruction
            {
                ProgramId = ComputeBudgetProgramId,
                Keys = new List<AccountMeta>(),
                Data = data,
            };
        }

        public static TransactionInstruction CreateAssociatedTokenAccountIdempotent(
            PublicKey payer, PublicKey owner, PublicKey mint)
        {
            var ata = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(owner, mint);
            return new TransactionInstruction
            {
                ProgramId = AssociatedTokenAccountProgram.ProgramIdKey,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.Writable(payer, true),
                    AccountMeta.Writable(ata, false),
                    AccountMeta.ReadOnly(owner, false),
                    AccountMeta.ReadOnly(mint, false),
                    AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
                    AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                },
                Data = new byte[] { 1 },
            };
        }
    }
}
