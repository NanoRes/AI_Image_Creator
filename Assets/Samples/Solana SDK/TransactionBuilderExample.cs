using Solana.Unity.Programs;
using Solana.Unity.Programs.Models;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Solana.Unity.Examples
{
    public class TransactionBuilderExample : IExample
    {
        private static readonly IRpcClient rpcClient = ClientFactory.GetClient(Cluster.TestNet);

        private const string MnemonicWords =
            "route clerk disease box emerge airport loud waste attitude film army tray " +
            "forward deal onion eight catalog surface unit card window walnut wealth medal";

        public async void Run()
        {
            Wallet.Wallet wallet = new Wallet.Wallet(MnemonicWords);

            Account fromAccount = wallet.GetAccount(10);
            Account toAccount = wallet.GetAccount(8);

            RequestResult<ResponseValue<BlockHash>> blockHash = await rpcClient.GetRecentBlockHashAsync();
            Console.WriteLine($"BlockHash >> {blockHash.Result.Value.Blockhash}");

            byte[] tx = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(fromAccount)
                .AddInstruction(SystemProgram.Transfer(fromAccount.PublicKey, toAccount.PublicKey, 10000000))
                .AddInstruction(MemoProgram.NewMemo(fromAccount.PublicKey, "NanoRes"))
                .Build(fromAccount);

            Console.WriteLine($"Tx base64: {Convert.ToBase64String(tx)}");
            RequestResult<ResponseValue<SimulationLogs>> txSim = await rpcClient.SimulateTransactionAsync(tx);
            string logs = Examples.PrettyPrintTransactionSimulationLogs(txSim.Result.Value.Logs);
            Console.WriteLine($"Transaction Simulation:\n\tError: {txSim.Result.Value.Error}\n\tLogs: \n" + logs);
            RequestResult<string> firstSig = await rpcClient.SendTransactionAsync(tx);
            Console.WriteLine($"First Tx Signature: {firstSig.Result}");
        }
    }

    public class CreateInitializeAndMintToExample : IExample
    {
        private static readonly IRpcClient rpcClient = ClientFactory.GetClient(Cluster.TestNet);

        private const string MnemonicWords =
            "route clerk disease box emerge airport loud waste attitude film army tray " +
            "forward deal onion eight catalog surface unit card window walnut wealth medal";

        public async void Run()
        {
            Wallet.Wallet wallet = new Wallet.Wallet(MnemonicWords);

            RequestResult<ResponseValue<BlockHash>> blockHash = await rpcClient.GetRecentBlockHashAsync();
            ulong minBalanceForExemptionAcc =
                (await rpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.TokenAccountDataSize)).Result;
            Console.WriteLine($"MinBalanceForRentExemption Account >> {minBalanceForExemptionAcc}");

            ulong minBalanceForExemptionMint =
                (await rpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.MintAccountDataSize)).Result;
            Console.WriteLine($"MinBalanceForRentExemption Mint Account >> {minBalanceForExemptionMint}");

            Account mintAccount = wallet.GetAccount(2222);
            Console.WriteLine($"MintAccount: {mintAccount}");
            Account ownerAccount = wallet.GetAccount(10);
            Console.WriteLine($"OwnerAccount: {ownerAccount}");
            Account initialAccount = wallet.GetAccount(3333);
            Console.WriteLine($"InitialAccount: {initialAccount}");

            byte[] tx = new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(ownerAccount)
                .AddInstruction(SystemProgram.CreateAccount(
                    ownerAccount.PublicKey,
                    mintAccount.PublicKey,
                    minBalanceForExemptionMint,
                    TokenProgram.MintAccountDataSize,
                    TokenProgram.ProgramIdKey))
                .AddInstruction(TokenProgram.InitializeMint(
                    mintAccount.PublicKey,
                    2,
                    ownerAccount.PublicKey,
                    ownerAccount.PublicKey))
                .AddInstruction(SystemProgram.CreateAccount(
                    ownerAccount,
                    initialAccount,
                    minBalanceForExemptionAcc,
                    TokenProgram.TokenAccountDataSize,
                    TokenProgram.ProgramIdKey))
                .AddInstruction(TokenProgram.InitializeAccount(
                    initialAccount.PublicKey,
                    mintAccount.PublicKey,
                    ownerAccount.PublicKey))
                .AddInstruction(TokenProgram.MintTo(
                    mintAccount.PublicKey,
                    initialAccount.PublicKey,
                    25000,
                    ownerAccount.PublicKey))
                .AddInstruction(MemoProgram.NewMemo(initialAccount.PublicKey, "NanoRes"))
                .Build(new List<Account> { ownerAccount, mintAccount, initialAccount });

            Console.WriteLine($"Tx: {Convert.ToBase64String(tx)}");

            RequestResult<ResponseValue<SimulationLogs>> txSim = await rpcClient.SimulateTransactionAsync(tx);
            string logs = Examples.PrettyPrintTransactionSimulationLogs(txSim.Result.Value.Logs);
            Console.WriteLine($"Transaction Simulation:\n\tError: {txSim.Result.Value.Error}\n\tLogs: \n" + logs);

            RequestResult<string> txReq = await rpcClient.SendTransactionAsync(tx);
            Console.WriteLine($"Tx Signature: {txReq.Result}");
        }
    }

    public class SimpleMintToExample : IExample
    {
        private static readonly IRpcClient rpcClient = ClientFactory.GetClient(Cluster.TestNet);

        private const string MnemonicWords =
            "route clerk disease box emerge airport loud waste attitude film army tray " +
            "forward deal onion eight catalog surface unit card window walnut wealth medal";

        public async void Run()
        {
            Wallet.Wallet wallet = new Wallet.Wallet(MnemonicWords);

            RequestResult<ResponseValue<BlockHash>> blockHash = await rpcClient.GetRecentBlockHashAsync();
            ulong minBalanceForExemptionAcc =
                (await rpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.TokenAccountDataSize)).Result;
            Console.WriteLine($"MinBalanceForRentExemption Account >> {minBalanceForExemptionAcc}");

            ulong minBalanceForExemptionMint =
                (await rpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.MintAccountDataSize)).Result;
            Console.WriteLine($"MinBalanceForRentExemption Mint Account >> {minBalanceForExemptionMint}");

            Account mintAccount = wallet.GetAccount(21);
            Console.WriteLine($"MintAccount: {mintAccount}");
            Account ownerAccount = wallet.GetAccount(10);
            Console.WriteLine($"OwnerAccount: {ownerAccount}");
            Account initialAccount = wallet.GetAccount(26);
            Console.WriteLine($"InitialAccount: {initialAccount}");

            byte[] tx = new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(ownerAccount)
                .AddInstruction(TokenProgram.MintTo(
                    mintAccount.PublicKey,
                    initialAccount.PublicKey,
                    25000000,
                    ownerAccount.PublicKey))
                .AddInstruction(MemoProgram.NewMemo(initialAccount.PublicKey, "NanoRes"))
                .Build(new List<Account> { ownerAccount, initialAccount });

            Console.WriteLine($"Tx: {Convert.ToBase64String(tx)}");

            RequestResult<ResponseValue<SimulationLogs>> txSim = await rpcClient.SimulateTransactionAsync(tx);
            string logs = Examples.PrettyPrintTransactionSimulationLogs(txSim.Result.Value.Logs);
            Console.WriteLine($"Transaction Simulation:\n\tError: {txSim.Result.Value.Error}\n\tLogs: \n" + logs);

            RequestResult<string> txReq = await rpcClient.SendTransactionAsync(tx);
            Console.WriteLine($"Tx Signature: {txReq.Result}");
        }
    }

    public class TransferTokenExample : IExample
    {
        private static readonly IRpcClient rpcClient = ClientFactory.GetClient(Cluster.TestNet);

        private const string MnemonicWords =
            "route clerk disease box emerge airport loud waste attitude film army tray " +
            "forward deal onion eight catalog surface unit card window walnut wealth medal";

        public async void Run()
        {
            Wallet.Wallet wallet = new Wallet.Wallet(MnemonicWords);

            RequestResult<ResponseValue<BlockHash>> blockHash = await rpcClient.GetRecentBlockHashAsync();
            ulong minBalanceForExemptionAcc = (await rpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.TokenAccountDataSize)).Result;
            Console.WriteLine($"MinBalanceForRentExemption Account >> {minBalanceForExemptionAcc}");

            Account mintAccount = wallet.GetAccount(31);
            Console.WriteLine($"MintAccount: {mintAccount}");
            Account ownerAccount = wallet.GetAccount(10);
            Console.WriteLine($"OwnerAccount: {ownerAccount}");
            Account initialAccount = wallet.GetAccount(32);
            Console.WriteLine($"InitialAccount: {initialAccount}");
            Account newAccount = wallet.GetAccount(33);
            Console.WriteLine($"NewAccount: {newAccount}");

            byte[] tx = new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(ownerAccount)
                .AddInstruction(SystemProgram.CreateAccount(
                    ownerAccount.PublicKey,
                    newAccount.PublicKey,
                    minBalanceForExemptionAcc,
                    TokenProgram.TokenAccountDataSize,
                    TokenProgram.ProgramIdKey))
                .AddInstruction(TokenProgram.InitializeAccount(
                    newAccount.PublicKey,
                    mintAccount.PublicKey,
                    ownerAccount.PublicKey))
                .AddInstruction(TokenProgram.Transfer(
                    initialAccount.PublicKey,
                    newAccount.PublicKey,
                    25000,
                    ownerAccount))
                .AddInstruction(MemoProgram.NewMemo(initialAccount, "NanoRes"))
                .Build(new List<Account> { ownerAccount, newAccount, initialAccount });

            Console.WriteLine($"Tx: {Convert.ToBase64String(tx)}");

            RequestResult<ResponseValue<SimulationLogs>> txSim = await rpcClient.SimulateTransactionAsync(tx);
            string logs = Examples.PrettyPrintTransactionSimulationLogs(txSim.Result.Value.Logs);
            Console.WriteLine($"Transaction Simulation:\n\tError: {txSim.Result.Value.Error}\n\tLogs: \n" + logs);

            RequestResult<string> txReq = await rpcClient.SendTransactionAsync(tx);
            Console.WriteLine($"Tx Signature: {txReq.Result}");
        }
    }

    public class TransferTokenCheckedExample : IExample
    {
        private static readonly IRpcClient rpcClient = ClientFactory.GetClient(Cluster.TestNet);

        private const string MnemonicWords =
            "route clerk disease box emerge airport loud waste attitude film army tray " +
            "forward deal onion eight catalog surface unit card window walnut wealth medal";

        public async void Run()
        {
            Wallet.Wallet wallet = new Wallet.Wallet(MnemonicWords);

            RequestResult<ResponseValue<BlockHash>> blockHash = await rpcClient.GetRecentBlockHashAsync();
            ulong minBalanceForExemptionAcc =
                (await rpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.TokenAccountDataSize)).Result;
            Console.WriteLine($"MinBalanceForRentExemption Account >> {minBalanceForExemptionAcc}");

            Account mintAccount = wallet.GetAccount(21);
            Console.WriteLine($"MintAccount: {mintAccount}");
            Account ownerAccount = wallet.GetAccount(10);
            Console.WriteLine($"OwnerAccount: {ownerAccount}");
            Account initialAccount = wallet.GetAccount(26);
            Console.WriteLine($"InitialAccount: {initialAccount}");
            Account newAccount = wallet.GetAccount(27);
            Console.WriteLine($"NewAccount: {newAccount}");

            byte[] tx = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(ownerAccount)
                .AddInstruction(SystemProgram.CreateAccount(
                        ownerAccount.PublicKey,
                        newAccount.PublicKey,
                        minBalanceForExemptionAcc,
                        TokenProgram.TokenAccountDataSize,
                        TokenProgram.ProgramIdKey))
                .AddInstruction(TokenProgram.InitializeAccount(
                        newAccount.PublicKey,
                        mintAccount.PublicKey,
                        ownerAccount.PublicKey))
                .AddInstruction(TokenProgram.TransferChecked(
                        initialAccount.PublicKey,
                        newAccount.PublicKey,
                        25000,
                        2,
                        ownerAccount,
                        mintAccount.PublicKey))
                .AddInstruction(MemoProgram.NewMemo(
                        initialAccount, "NanoRes"))
                .Build(new List<Account> { ownerAccount, newAccount, initialAccount });

            Console.WriteLine($"Tx: {Convert.ToBase64String(tx)}");

            RequestResult<ResponseValue<SimulationLogs>> txSim = await rpcClient.SimulateTransactionAsync(tx);
            string logs = Examples.PrettyPrintTransactionSimulationLogs(txSim.Result.Value.Logs);
            Console.WriteLine($"Transaction Simulation:\n\tError: {txSim.Result.Value.Error}\n\tLogs: \n" + logs);

            RequestResult<string> txReq = await rpcClient.SendTransactionAsync(tx);
            Console.WriteLine($"Tx Signature: {txReq.Result}");
        }
    }

    public class CreateNonceAccountExample : IExample
    {
        private static readonly IRpcClient rpcClient = ClientFactory.GetClient(Cluster.TestNet);
        private const string MnemonicWords =
            "route clerk disease box emerge airport loud waste attitude film army tray " +
            "forward deal onion eight catalog surface unit card window walnut wealth medal";

        public async void Run()
        {
            Wallet.Wallet wallet = new Wallet.Wallet(MnemonicWords);

            RequestResult<ResponseValue<BlockHash>> blockHash = await rpcClient.GetRecentBlockHashAsync();
            ulong minBalanceForExemptionAcc =
                (await rpcClient.GetMinimumBalanceForRentExemptionAsync(NonceAccount.AccountDataSize)).Result;

            Account ownerAccount = wallet.GetAccount(10);
            Console.WriteLine($"OwnerAccount: {ownerAccount}");
            Account nonceAccount = wallet.GetAccount(1119);
            Console.WriteLine($"NonceAccount: {nonceAccount}");
            Account newAuthority = wallet.GetAccount(1129);
            Console.WriteLine($"NewAuthority: {newAuthority}");

            byte[] tx = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(ownerAccount)
                .AddInstruction(SystemProgram.CreateAccount(
                    ownerAccount.PublicKey,
                    nonceAccount.PublicKey,
                    minBalanceForExemptionAcc,
                    NonceAccount.AccountDataSize,
                    SystemProgram.ProgramIdKey
                ))
                .AddInstruction(SystemProgram.InitializeNonceAccount(
                    nonceAccount,
                    ownerAccount))
                .Build(new List<Account> { ownerAccount, nonceAccount });


            Console.WriteLine($"Tx: {Convert.ToBase64String(tx)}");
            RequestResult<ResponseValue<SimulationLogs>> txSim = await rpcClient.SimulateTransactionAsync(tx);
            string logs = Examples.PrettyPrintTransactionSimulationLogs(txSim.Result.Value.Logs);
            Console.WriteLine($"Transaction Simulation:\n\tError: {txSim.Result.Value.Error}\n\tLogs: \n" + logs);

            RequestResult<string> txReq = await rpcClient.SendTransactionAsync(tx);
            Console.WriteLine($"Tx Signature: {txReq.Result}");
        }
    }

    public class TransactionBuilderTransferWithDurableNonceExample : IExample
    {
        private static readonly IRpcClient rpcClient = ClientFactory.GetClient(Cluster.TestNet);
        private const string MnemonicWords =
            "route clerk disease box emerge airport loud waste attitude film army tray " +
            "forward deal onion eight catalog surface unit card window walnut wealth medal";

        public async void Run()
        {
            Wallet.Wallet wallet = new Wallet.Wallet(MnemonicWords);

            Account ownerAccount = wallet.GetAccount(10);
            Console.WriteLine($"OwnerAccount: {ownerAccount}");
            Account nonceAccount = wallet.GetAccount(1119);
            Console.WriteLine($"NonceAccount: {nonceAccount}");
            Account toAccount = wallet.GetAccount(1);
            Console.WriteLine($"ToAccount: {toAccount}");

            // Get the Nonce Account to get the Nonce to use for the transaction
            RequestResult<ResponseValue<AccountInfo>> nonceAccountInfo = await rpcClient.GetAccountInfoAsync(nonceAccount.PublicKey);
            byte[] accountDataBytes = Convert.FromBase64String(nonceAccountInfo.Result.Value.Data[0]);
            NonceAccount nonceAccountData = NonceAccount.Deserialize(accountDataBytes);
            Console.WriteLine($"NonceAccount Authority: {nonceAccountData.Authorized.Key}");
            Console.WriteLine($"NonceAccount Nonce: {nonceAccountData.Nonce.Key}");

            // Initialize the nonce information to be used with the transaction
            NonceInformation nonceInfo = new NonceInformation()
            {
                Nonce = nonceAccountData.Nonce,
                Instruction = SystemProgram.AdvanceNonceAccount(
                    nonceAccount.PublicKey,
                    ownerAccount
                )
            };

            byte[] tx = new TransactionBuilder()
                .SetFeePayer(ownerAccount)
                .SetNonceInformation(nonceInfo)
                .AddInstruction(
                    SystemProgram.Transfer(
                        ownerAccount,
                        toAccount,
                        1_000_000_000)
                )
                .Build(ownerAccount);

            Console.WriteLine($"Tx: {Convert.ToBase64String(tx)}");
            RequestResult<ResponseValue<SimulationLogs>> txSim = await rpcClient.SimulateTransactionAsync(tx);
            string logs = Examples.PrettyPrintTransactionSimulationLogs(txSim.Result.Value.Logs);
            Console.WriteLine($"Transaction Simulation:\n\tError: {txSim.Result.Value.Error}\n\tLogs: \n" + logs);

            RequestResult<string> txReq = await rpcClient.SendTransactionAsync(tx);
            Console.WriteLine($"Tx Signature: {txReq.Result}");
        }
    }

    public class BurnExample : IExample
    {
        private static readonly IRpcClient rpcClient = ClientFactory.GetClient(Cluster.TestNet);

        private const string MnemonicWords =
            "route clerk disease box emerge airport loud waste attitude film army tray " +
            "forward deal onion eight catalog surface unit card window walnut wealth medal";

        public async void Run()
        {
            Wallet.Wallet wallet = new Wallet.Wallet(MnemonicWords);

            RequestResult<ResponseValue<BlockHash>> blockHash = await rpcClient.GetRecentBlockHashAsync();

            ulong minBalanceForExemptionMultiSig =
                (await rpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.MultisigAccountDataSize)).Result;
            Console.WriteLine($"MinBalanceForRentExemption MultiSig >> {minBalanceForExemptionMultiSig}");
            ulong minBalanceForExemptionAcc =
                (await rpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.TokenAccountDataSize)).Result;
            Console.WriteLine($"MinBalanceForRentExemption Account >> {minBalanceForExemptionAcc}");
            ulong minBalanceForExemptionMint =
                (await rpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.MintAccountDataSize)).Result;
            Console.WriteLine($"MinBalanceForRentExemption Mint Account >> {minBalanceForExemptionMint}");

            Account ownerAccount = wallet.GetAccount(10);
            Account mintAccount = wallet.GetAccount(21);
            Account initialAccount = wallet.GetAccount(26);

            byte[] msgData = new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(ownerAccount)
                .AddInstruction(TokenProgram.Burn(
                    initialAccount.PublicKey,
                    mintAccount.PublicKey,
                    200,
                    ownerAccount))
                .AddInstruction(MemoProgram.NewMemo(ownerAccount, "NanoRes"))
                .CompileMessage();

            Message msg = Examples.DecodeMessageFromWire(msgData);

            Console.WriteLine("\n\tPOPULATING TRANSACTION WITH SIGNATURES\t");
            Transaction tx = Transaction.Populate(msg,
                new List<byte[]> { ownerAccount.Sign(msgData) });

            byte[] txBytes = Examples.LogTransactionAndSerialize(tx);

            string mintToSignature = await Examples.SubmitTxSendAndLog(txBytes);
            Examples.PollConfirmedTx(mintToSignature);
        }
    }

    public class AddSignatureExample : IExample
    {
        private static readonly IRpcClient rpcClient = ClientFactory.GetClient(Cluster.TestNet);

        private const string MnemonicWords =
            "route clerk disease box emerge airport loud waste attitude film army tray " +
            "forward deal onion eight catalog surface unit card window walnut wealth medal";

        public async void Run()
        {
            Wallet.Wallet wallet = new Wallet.Wallet(MnemonicWords);

            Account fromAccount = wallet.GetAccount(10);
            Account toAccount = wallet.GetAccount(8);

            RequestResult<ResponseValue<BlockHash>> blockHash = await rpcClient.GetRecentBlockHashAsync();
            Console.WriteLine($"BlockHash >> {blockHash.Result.Value.Blockhash}");

            TransactionBuilder txBuilder = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(fromAccount)
                .AddInstruction(SystemProgram.Transfer(fromAccount.PublicKey, toAccount.PublicKey, 10000000))
                .AddInstruction(MemoProgram.NewMemo(fromAccount.PublicKey, "NanoRes"));

            byte[] msgBytes = txBuilder.CompileMessage();
            byte[] signature = fromAccount.Sign(msgBytes);

            byte[] tx = txBuilder.AddSignature(signature)
                .Serialize();

            Console.WriteLine($"Tx base64: {Convert.ToBase64String(tx)}");
            RequestResult<ResponseValue<SimulationLogs>> txSim = await rpcClient.SimulateTransactionAsync(tx);
            string logs = Examples.PrettyPrintTransactionSimulationLogs(txSim.Result.Value.Logs);
            Console.WriteLine($"Transaction Simulation:\n\tError: {txSim.Result.Value.Error}\n\tLogs: \n" + logs);
            RequestResult<string> firstSig = await rpcClient.SendTransactionAsync(tx);
            Console.WriteLine($"First Tx Signature: {firstSig.Result}");
        }
    }

}