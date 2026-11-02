using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public class FPSLogger : MonoBehaviour
{
	[SerializeField]
	private float loggingDurationSeconds = 60f;

	[SerializeField]
	private string subdirectoryName = "SimulationLogs";

	private float loggingStartTime;
	private StreamWriter streamWriter;
	private string logFilePath;
	private bool isLoggingActive;

	private void Start()
	{
		loggingStartTime = Time.time;
		InitializeWriter();
		isLoggingActive = streamWriter != null;
	}

	private void Update()
	{
		if (!isLoggingActive)
		{
			return;
		}

		float elapsedSeconds = Time.time - loggingStartTime;
		if (elapsedSeconds >= loggingDurationSeconds)
		{
			FinishLogging();
			return;
		}

		float deltaTimeSeconds = Time.deltaTime;
		float fps = deltaTimeSeconds > 0f ? 1f / deltaTimeSeconds : 0f;

		// timestamp (since game start), fps, frame_time_ms
		string line = string.Concat(
			elapsedSeconds.ToString(CultureInfo.InvariantCulture), ",",
			fps.ToString(CultureInfo.InvariantCulture), ",",
			(deltaTimeSeconds * 1000f).ToString(CultureInfo.InvariantCulture)
		);
		streamWriter.WriteLine(line);
	}

	private void OnDestroy()
	{
		FinishLogging();
	}

	private void InitializeWriter()
	{
		try
		{
			// Save inside the Unity project folder (next to Assets)
			string projectRoot = Directory.GetParent(Application.dataPath).FullName;
			string directory = string.IsNullOrEmpty(subdirectoryName) ? projectRoot : Path.Combine(projectRoot, subdirectoryName);
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
			string fileName = $"fps_log_{timestamp}.csv";
			logFilePath = Path.Combine(directory, fileName);

			streamWriter = new StreamWriter(logFilePath, append: false)
			{
				AutoFlush = true
			};

			// CSV header
			streamWriter.WriteLine("time_s,fps,frame_time_ms");
		}
		catch (Exception e)
		{
			Debug.LogError($"FPSLogger failed to open log file: {e.Message}");
			streamWriter = null;
		}
	}

	private void FinishLogging()
	{
		if (!isLoggingActive)
		{
			return;
		}

		isLoggingActive = false;
		try
		{
			if (streamWriter != null)
			{
				streamWriter.Flush();
				streamWriter.Dispose();
				streamWriter = null;
			}
			if (!string.IsNullOrEmpty(logFilePath))
			{
				Debug.Log($"FPS log written to: {logFilePath}");
			}
		}
		catch (Exception e)
		{
			Debug.LogWarning($"FPSLogger encountered an error while finishing logging: {e.Message}");
		}
		finally
		{
			// Disable the component after finishing
			enabled = false;
		}
	}
}


