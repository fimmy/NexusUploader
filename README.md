# NexusUploader
Batch upload asset to  Nexus Repository Manager 3
## Usage
```
NexusUploader.exe [options] <upload dir>

Arguments:
  upload dir                 Upload Base Directory Path

Options:
  -?|-h|--help               Show help information.
  -r|--host <REPOHOST>       Nexus Host Url
  -n|--repo-name <REPONAME>  Nexus Repository Name
  -t|--repo-type <REPOTYPE>  Nexus Repository Type
                             Allowed values are: maven, npm, nuget.
  -u|--user <USER>           Nexus User
  -p|--password <PASSWORD>   Nexus Password
```
