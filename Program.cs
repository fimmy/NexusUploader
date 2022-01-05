using McMaster.Extensions.CommandLineUtils;
using Flurl.Http;
using Flurl;

var app = new CommandLineApplication();

app.HelpOption();
var path = app.Argument<string>("upload dir", "Upload Base Directory Path").Accepts(x => x.ExistingDirectory());
path.DefaultValue = AppContext.BaseDirectory;
var host = app.Option<string>("-r|--host <REPOHOST>", "Nexus Host Url", CommandOptionType.SingleValue).IsRequired(true);
var repoName = app.Option<string>("-n|--repo-name <REPONAME>", "Nexus Repository Name", CommandOptionType.SingleValue).IsRequired(true);
var repoType = app.Option<RepoType>("-t|--repo-type <REPOTYPE>", "Nexus Repository Type", CommandOptionType.SingleValue).IsRequired(true).Accepts(x => x.Enum<RepoType>());
var user = app.Option<string>("-u|--user <USER>", "Nexus User", CommandOptionType.SingleValue);
var password = app.Option<string>("-p|--password <PASSWORD>", "Nexus Password", CommandOptionType.SingleValue);


app.OnExecuteAsync(async cancellationToken =>
{
    Console.WriteLine($"Upload Base Directory Path: {path.ParsedValue}");
    Console.WriteLine($"Nexus Host Url: {host.ParsedValue}");
    Console.WriteLine($"Nexus Repository Name: {repoName.ParsedValue}");
    Console.WriteLine($"Nexus Repository Type: {repoType.ParsedValue}");
    Console.WriteLine("Start Uploading");
    switch (repoType.ParsedValue)
    {
        case RepoType.maven:
            await uploadMavenAsync(cancellationToken, path.ParsedValue, host.ParsedValue, repoName.ParsedValue, user.ParsedValue, password.ParsedValue);
            break;
        case RepoType.npm:
            await uploadNpmAsync(cancellationToken, path.ParsedValue, host.ParsedValue, repoName.ParsedValue, user.ParsedValue, password.ParsedValue);
            break;
        default:
            break;
    }
    Console.WriteLine("Upload Completed");
});
return app.Execute(args);

async Task uploadMavenAsync(CancellationToken cancellationToken, string dir, string host, string repoName, string user, string password)
{
    var jarFiles = Directory.EnumerateFiles(dir, "*.jar", SearchOption.AllDirectories);
    Console.WriteLine($"Total jar file count: {jarFiles.Count()}");
    if (jarFiles.Any())
    {
        if (string.IsNullOrEmpty(user))
        {
            user = Prompt.GetString("Nexus User Name: ");
        }
        if (string.IsNullOrEmpty(password))
        {
            password = Prompt.GetPassword("Nexus Password: ");
        }

        foreach (var jarFileFullPath in jarFiles)
        {
            Console.WriteLine($"uploading file: {jarFileFullPath}");
            var jarFileInfo = new FileInfo(jarFileFullPath);
            var jarFileName = jarFileInfo.Name;
            var jarFileNameWithoutExt = Path.GetFileNameWithoutExtension(jarFileName);
            var pomFileName = $"{jarFileNameWithoutExt}.pom";
            var pomFileFullPath = Path.Combine(jarFileInfo.DirectoryName ?? "", pomFileName);
            var pomFileInfo = new FileInfo(pomFileFullPath);
            if (pomFileInfo.Exists)
            {
                var response = await host.AppendPathSegment("/service/rest/v1/components").SetQueryParams(new
                {
                    repository = repoName
                }).AllowAnyHttpStatus().WithBasicAuth(user, password).PostMultipartAsync(mp =>
                    mp.AddString("maven2.asset1.extension", "jar")
                    .AddFile("maven2.asset1", jarFileFullPath)
                    .AddString("maven2.asset2.extension", "pom")
                    .AddFile("maven2.asset2", pomFileFullPath), cancellationToken);
                if (response.StatusCode == 204)
                {
                    Console.WriteLine($"upload success: {await response.GetStringAsync()}");
                }
                else
                {
                    Console.WriteLine($"upload failed:{response.StatusCode}-{await response.GetStringAsync()}");
                }
            }
            else
            {
                Console.WriteLine($"{pomFileFullPath} not exist, skip upload");
            }

        }
    }
    else
    {
        Console.WriteLine("No any maven package");
    }
}

async Task uploadNpmAsync(CancellationToken cancellationToken, string dir, string host, string repoName, string user, string password)
{
    var tgzFiles = Directory.EnumerateFiles(dir, "*.tgz", SearchOption.AllDirectories);
    Console.WriteLine($"Total tgz file count: {tgzFiles.Count()}");
    if (tgzFiles.Any())
    {
        if (string.IsNullOrEmpty(user))
        {
            user = Prompt.GetString("Nexus User Name: ")??"";
        }
        if (string.IsNullOrEmpty(password))
        {
            password = Prompt.GetPassword("Nexus Password: ");
        }

        foreach (var tgzFileFullPath in tgzFiles)
        {
            Console.WriteLine($"uploading file: {tgzFileFullPath}");

            var response = await host.AppendPathSegment("/service/rest/v1/components").SetQueryParams(new
            {
                repository = repoName
            }).AllowAnyHttpStatus().WithBasicAuth(user, password).PostMultipartAsync(mp =>
                mp.AddFile("npm.asset", tgzFileFullPath), cancellationToken);
            if (response.StatusCode == 204)
            {
                Console.WriteLine($"upload success: {await response.GetStringAsync()}");
            }
            else
            {
                Console.WriteLine($"upload failed:{response.StatusCode}-{await response.GetStringAsync()}");
            }

        }
    }
    else
    {
        Console.WriteLine("No any npm package");
    }
}

public enum RepoType
{
    maven,
    npm
}