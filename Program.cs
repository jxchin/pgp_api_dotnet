using PGP_API;
using System.Text.Json;

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

app.MapPost("/encrypt", async (HttpRequest request) =>
{
    // Check Filename is provided.
    string? name = "";
    if (name == null)
    {
        throw new Exception("Filename is empty.");
    }
    //IFormCollection form = request.Form;

    using StreamReader reader = new StreamReader(request.Body);
    string bodyStr = await reader.ReadToEndAsync();

    ReqBody? jsonStr = JsonSerializer.Deserialize<ReqBody>(bodyStr);

    name = jsonStr?.Name;
        
    // Add Logging later.
      
    Utility Utility = new Utility();

    Stream sourceStream = Utility.DownloadIngressFile(name);

    using Stream encryptedStream = new MemoryStream();
    Utility.PGPEncrypt(sourceStream, encryptedStream);
    string status = Utility.UploadEncryptedFile(encryptedStream, name);
    
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
