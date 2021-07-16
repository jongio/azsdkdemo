using System;
using System.IO;
using DotNetEnv;

namespace Lib.Configuration
{
    public static class Envs
    {
        public static void Load()
        {
            // If we have DOTENV_FULLPATH, then the app has passed in the full path to the .env file, so just use that.
            string dotEnvFullPath = Environment.GetEnvironmentVariable("DOTENV_FULLPATH");
            if (!string.IsNullOrEmpty(dotEnvFullPath))
            {
                Env.Load(dotEnvFullPath);
            }
            else
            {
                // We don't have DOTENV_FULLPATH, so let's try to find the .env file.  
                // The app can override the default .env file name with DOTENV_FILENAME env var
                
                string dotEnvFileNameEnv = Environment.GetEnvironmentVariable("DOTENV_FILENAME");
                string dotEnvFileName = !String.IsNullOrEmpty(dotEnvFileNameEnv) ? dotEnvFileNameEnv : DotNetEnv.Env.DEFAULT_ENVFILENAME;
                Env.TraversePath().Load(dotEnvFileName);
            }
        }
    }
}