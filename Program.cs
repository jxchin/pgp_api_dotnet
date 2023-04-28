using PGP_API;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/encrypt", (PGP_API.ReqBody body) =>
{
    // Check Filename is provided.
    if (body.Name == null)
    {
        throw new Exception("Filename is empty.");
    }
    // Add Logging later.

    Utility Utility = new Utility();

    Stream sourceStream = Utility.DownloadIngressFile(body.Name);

    using Stream encryptedStream = new MemoryStream();
    Utility.PGPEncrypt(sourceStream, encryptedStream);
    var status = Utility.UploadEncryptedFile(encryptedStream, body.Name);
    
    // Todo: return status failed and error message or status success in json format
    return status;
})
.WithName("EncryptFile")
.WithOpenApi();

app.MapGet("decrypt", (PGP_API.ReqBody body) =>
{
    // Check Filename is provided.
    if (body.Name == null)
    {
        throw new Exception("Filename is empty.");
    }
    // Add Logging later.

    Utility Utility = new Utility();

    Stream sourceStream = Utility.DownloadEgressFile(body.Name);

    using Stream decryptedStream = new MemoryStream();
    Utility.PGPDecrypt(sourceStream, decryptedStream);
    var status = Utility.UploadDecryptedFile(decryptedStream, body.Name);

    return status;
})
.WithName("DecryptFile")
.WithOpenApi();


app.Run();
