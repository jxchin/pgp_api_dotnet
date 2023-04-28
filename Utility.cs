using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using PgpCore;
using System.Text;

namespace PGP_API
{
    public class Utility
    {
        private TokenCredential _msiCredential;
        private BlobContainerClient _container;
        private SecretClient _kvClient;

        public Utility()
        {
            /** 
             * Instantiate MSI Credential to authenticate with Storage Account and KeyVault
             * Please ensure Managed Identity is enabled and given access to above services
             * 'Storage Blob Data Contributor' to Storage Account
             * 'Key Vault Secrets User' to Key Vault
            **/

            // Todo: Parameterise values
            string kvUri = "https://kvt-cpf-udp-mdev.vault.azure.net";
            string saUri = "https://sacjxpgpfunapp.blob.core.windows.net/samples-workitems";

            _msiCredential = new DefaultAzureCredential();
            _container = new BlobContainerClient(new Uri(saUri), _msiCredential);
            _kvClient = new SecretClient(new Uri(kvUri), _msiCredential);
        }

        public Stream DownloadIngressFile(string filename)
        {
            using Stream sourceStream = new MemoryStream();

            // Set Source path in Storage Account
            string sourcePath = string.Concat("in/", filename);
            var sourceBlob = _container.GetBlobClient(sourcePath);

            // Download file to Memory
            sourceBlob.DownloadTo(sourceStream);

            return sourceStream;
        }

        public Stream DownloadEgressFile(string filename)
        {
            using Stream sourceStream = new MemoryStream();

            // Set Source path in Storage Account
            string sourcePath = string.Concat("out/", filename);
            var sourceBlob = _container.GetBlobClient(sourcePath);

            // Download file to Memory
            sourceBlob.DownloadTo(sourceStream);

            return sourceStream;
        }

        public string UploadEncryptedFile(Stream stream, string filename)
        {
            string status = "Success";

            // Change the file extension to pgp
            string targetBlobName = filename.Substring(0, filename.Length - 3);
            string targetPath = string.Concat("/out_encrypted/", targetBlobName, "pgp");
            // Set Destination in Storage Account
            BlobClient targetBlob = _container.GetBlobClient(targetPath);

            // Save the encrypted stream as a file in Storage Account
            targetBlob.Upload(stream);

            return status;
        }

        public string UploadDecryptedFile(Stream stream, string filename)
        {
            string status = "Success";

            // Change the file extension to pgp
            string targetBlobName = filename.Substring(0, filename.Length - 3);
            string targetPath = string.Concat("/in_decrypted/", targetBlobName, "pgp");
            // Set Destination in Storage Account
            BlobClient targetBlob = _container.GetBlobClient(targetPath);

            // Save the encrypted stream as a file in Storage Account
            targetBlob.Upload(stream);

            return status;
        }

        public string GetPGPPubKey()
        {
            // Todo: Parameterise values
            // Instantiate Key Vault Client
            string secretName = "udp-pgp-pub-key-cjx";

            // Get PGP Keys from Key Vault
            var secret = _kvClient.GetSecret(secretName);
            string pgpPubKey = Encoding.UTF8.GetString(Convert.FromBase64String(secret.Value.Value));

            return pgpPubKey;
        }

        public Tuple<string, string> GetPGPPrvKey()
        {
            string prvKeySecretName = "udp-pgp-prv-key-cjx";
            string passphraseSecretName = "udp-pgp-secret";

            var prvKeySecret = _kvClient.GetSecret(prvKeySecretName);
            var passphraseSecret = _kvClient.GetSecret(passphraseSecretName);

            string pgpPrvKey = Encoding.UTF8.GetString(Convert.FromBase64String(prvKeySecret.Value.Value));
            string passphrase = Encoding.UTF8.GetString(Convert.FromBase64String(passphraseSecret.Value.Value));

            var Resp = Tuple.Create(pgpPrvKey, passphrase);

            return Resp;
        }

        public void PGPEncrypt(Stream source, Stream encrypted)
        {
            // Get PGP Public Key from Key Vault
            string pgpPubKey = GetPGPPubKey();

            EncryptionKeys encryptionKeys = new EncryptionKeys(pgpPubKey);

            PGP pgp = new PGP(encryptionKeys);

            pgp.EncryptStream(source, encrypted);

            // Reset Stream pointer to beginning
            encrypted.Seek(0, SeekOrigin.Begin);
        }

        public void PGPDecrypt(Stream source, Stream decrypted)
        {
            var pgpPrvKey = GetPGPPrvKey();

            // Load keys
            EncryptionKeys encryptionKeys = new EncryptionKeys(pgpPrvKey.Item1, pgpPrvKey.Item2);

            PGP pgp = new PGP(encryptionKeys);

            pgp.DecryptStream(source, decrypted);

            // Reset Stream pointer to beginning
            decrypted.Seek(0, SeekOrigin.Begin);
        }
    }
}
