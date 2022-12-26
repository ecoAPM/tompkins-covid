using Microsoft.Extensions.Configuration;

namespace TompkinsCOVID;

public sealed class Runner
{
	private readonly ITwitter _twitter;
	private readonly IHealthDepartment _healthDept;
	private readonly Action<string> _log;
	private readonly string _username;
	private readonly TimeSpan _wait;

	public Runner(ITwitter twitter, IHealthDepartment healthDept, Action<string> log, IConfiguration config)
	{
		_twitter = twitter;
		_healthDept = healthDept;
		_log = log;
		_username = config["username"] ?? string.Empty;
		_wait = TimeSpan.FromSeconds(double.Parse(config["wait"] ?? string.Empty));
	}

	public async Task Run(string? arg = null)
	{
		_log("");
		var latest = DateOnly.TryParse(arg, out var argDate)
			? argDate
			: await _twitter.GetLatestPostedDate(_username);
		_log($"Last posted: {latest?.ToShortDateString() ?? "[never]"}");
		var recordDate = latest?.AddDays(-ActiveCaseCalculator.ActiveDays * 2)
			?? DateOnly.FromDateTime(DateTime.UnixEpoch);

		_log("");
		var records = await _healthDept.GetRecordsSince(recordDate);
		_log($"{records.Count} records found, for {records.LastOrDefault().Key.ToShortDateString()} through {records.FirstOrDefault().Key.ToShortDateString()}");

		var toTweet = records
			.Where(r => ShouldTweet(r.Value, latest))
			.OrderBy(r => r.Key)
			.ToArray();

		if (!toTweet.Any())
		{
			_log("Nothing to tweet!");
		}

		foreach (var record in toTweet)
		{
			record.Value.ActiveCases ??= records.CalculateActiveCases(record.Key);

			_log($"\nTweeting:\n{record.Value}\n");
			await _twitter.Tweet(record.Value);
			await Task.Delay(_wait);
		}

		_log("");
		_log("Done!");
	}

	private static bool ShouldTweet(Record r, DateOnly? latest)
		=> r.Date > latest && r.PositiveToday is not null;
}