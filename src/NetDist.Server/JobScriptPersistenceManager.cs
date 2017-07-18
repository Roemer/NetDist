using System;
using System.Collections.Generic;
using System.IO;

using NetDist.Core;
using NetDist.Logging;

namespace NetDist.Server
{
	public class JobScriptPersistenceManager
	{
		private static readonly string EnabledScriptsLocation = Path.Combine(Directory.GetCurrentDirectory(), "jobscripts", "enabled");
		private static readonly string DisabledScriptsLocation = Path.Combine(Directory.GetCurrentDirectory(), "jobscripts", "disabled");
		private Logger Logger { get; set; }

		public JobScriptPersistenceManager(Logger logger)
		{
			CreateDirectories();
			Logger = logger;
		}

		public List<JobScriptInfo> GetSavedJobScripts()
		{
			var result = new List<JobScriptInfo>();

			foreach (string file in Directory.EnumerateFiles(EnabledScriptsLocation, "*.cs"))
			{
				result.Add(new JobScriptInfo { JobScript = File.ReadAllText(file), AddedScriptFromSaved = true});
			}

			foreach (string file in Directory.EnumerateFiles(DisabledScriptsLocation, "*.cs"))
			{
				result.Add(new JobScriptInfo { JobScript = File.ReadAllText(file), IsDisabled = true, AddedScriptFromSaved = true});
			}

			return result;
		}

		public void SaveJobScript(string name, JobScriptInfo jobScript)
		{
			try
			{
				var destination = jobScript.IsDisabled ? DisabledScriptsLocation : EnabledScriptsLocation;
				var path = GetJobScriptPath(destination, name);
				File.WriteAllText(path, jobScript.JobScript);
			}
			catch(Exception ex)
			{
				Logger.Warn(ex, "Could not persist jobscript.");
			}
		}
		
		public void EnableJobScript(string jobScriptName)
		{
			MoveJobScript(jobScriptName, DisabledScriptsLocation, EnabledScriptsLocation);
		}

		public void DisableJobScript(string jobScriptName)
		{
			MoveJobScript(jobScriptName, EnabledScriptsLocation, DisabledScriptsLocation);
		}

		public void DeleteJobScript(string jobScriptName)
		{
			try
			{
				File.Delete(GetJobScriptPath(EnabledScriptsLocation, jobScriptName));
				File.Delete(GetJobScriptPath(DisabledScriptsLocation, jobScriptName));
			}
			catch (Exception ex)
			{
				Logger.Warn(ex, "Could not delete saved jobscript file.");
			}
		}
		
		private void MoveJobScript(string jobScriptName, string sourcePath, string destinationPath)
		{
			try
			{
				var sourceFile = GetJobScriptPath(sourcePath, jobScriptName);
				var destinationFile = GetJobScriptPath(destinationPath, jobScriptName);

				if (File.Exists(sourceFile))
				{

					if (File.Exists(destinationFile))
					{
						File.Delete(destinationFile);
					}

					File.Move(sourceFile, destinationFile);
				}
			}
			catch(Exception ex)
			{
				Logger.Warn(ex, "Could not move saved jobscript file.");
			}
		}

		private void CreateDirectories()
		{
			if (!Directory.Exists(EnabledScriptsLocation))
			{
				Directory.CreateDirectory(EnabledScriptsLocation);
			}

			if (!Directory.Exists(DisabledScriptsLocation))
			{
				Directory.CreateDirectory(DisabledScriptsLocation);
			}
		}

		private string GetJobScriptPath(string path, string fileName)
		{
			return Path.Combine(path, fileName + ".cs");
		}
	}
}
