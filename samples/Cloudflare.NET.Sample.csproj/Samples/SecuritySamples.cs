namespace Cloudflare.NET.SampleCoreConsole.Samples;

using Microsoft.Extensions.Logging;
using Security.Firewall.Models;

public class SecuritySamples(ICloudflareApiClient cf, ILogger<SecuritySamples> logger)
{
  #region Methods

  public async Task<List<Func<Task>>> RunZoneFirewallSamplesAsync(string zoneId)
  {
    var cleanupActions = new List<Func<Task>>();
    var ipToBlock      = "198.51.100.1"; // An IP from TEST-NET-2 documentation block

    // 1. Zone IP Access Rule Lifecycle
    logger.LogInformation("--- Running Zone IP Access Rule Lifecycle for IP {IP} ---", ipToBlock);

    var createZoneRuleRequest = new CreateAccessRuleRequest(
      AccessRuleMode.Block,
      new IpConfiguration(ipToBlock),
      "cfnet-sample-zone-rule");

    var zoneRule = await cf.Zones.AccessRules.CreateAsync(zoneId, createZoneRuleRequest);
    logger.LogInformation("Created Zone Access Rule {Id} to block {IP}", zoneRule.Id, ipToBlock);

    cleanupActions.Add(async () =>
    {
      logger.LogInformation("Deleting Zone Access Rule: {Id}", zoneRule.Id);
      await cf.Zones.AccessRules.DeleteAsync(zoneId, zoneRule.Id);
      logger.LogInformation("Deleted Zone Access Rule: {Id}", zoneRule.Id);
    });

    var fetchedZoneRule = await cf.Zones.AccessRules.GetAsync(zoneId, zoneRule.Id);

    logger.LogInformation("Fetched rule {Id}, Mode: {Mode}, Target: {Target}, Value: {Value}",
                          fetchedZoneRule.Id, fetchedZoneRule.Mode, fetchedZoneRule.Configuration.Target,
                          fetchedZoneRule.Configuration.Value);

    // 2. Zone Lockdown Rule Lifecycle
    logger.LogInformation("--- Running Zone Lockdown Rule Lifecycle ---");

    var lockdownUrl = $"*.example.com/admin";

    var createLockdownRequest = new CreateLockdownRequest(
      [lockdownUrl],
      [new(LockdownTarget.Ip, ipToBlock)],
      Description: "cfnet-sample-lockdown");

    var lockdownRule = await cf.Zones.Lockdown.CreateAsync(zoneId, createLockdownRequest);
    logger.LogInformation("Created Zone Lockdown rule {Id} for URL {Url}", lockdownRule.Id, lockdownUrl);

    cleanupActions.Add(async () =>
    {
      logger.LogInformation("Deleting Zone Lockdown rule: {Id}", lockdownRule.Id);
      await cf.Zones.Lockdown.DeleteAsync(zoneId, lockdownRule.Id);
      logger.LogInformation("Deleted Zone Lockdown rule: {Id}", lockdownRule.Id);
    });

    // 3. User-Agent Block Rule Lifecycle
    logger.LogInformation("--- Running User-Agent Block Rule Lifecycle ---");

    var uaToBlock = $"BadBot-{Guid.NewGuid():N}";

    var createUaRuleRequest = new CreateUaRuleRequest(
      UaRuleMode.Block,
      new UaRuleConfiguration("ua", uaToBlock),
      Description: "cfnet-sample-ua-block");

    var uaRule = await cf.Zones.UaRules.CreateAsync(zoneId, createUaRuleRequest);
    logger.LogInformation("Created User-Agent Block rule {Id} for UA '{UA}'", uaRule.Id, uaToBlock);

    cleanupActions.Add(async () =>
    {
      logger.LogInformation("Deleting User-Agent Block rule: {Id}", uaRule.Id);
      await cf.Zones.UaRules.DeleteAsync(zoneId, uaRule.Id);
      logger.LogInformation("Deleted User-Agent Block rule: {Id}", uaRule.Id);
    });

    return cleanupActions;
  }

  public async Task<List<Func<Task>>> RunAccountFirewallSamplesAsync()
  {
    var cleanupActions = new List<Func<Task>>();
    var ipToChallenge  = "198.51.100.2"; // Another IP from TEST-NET-2

    // 1. Account IP Access Rule Lifecycle
    logger.LogInformation("--- Running Account IP Access Rule Lifecycle for IP {IP} ---", ipToChallenge);

    var createAccountRuleRequest = new CreateAccessRuleRequest(
      AccessRuleMode.Challenge,
      new IpConfiguration(ipToChallenge),
      "cfnet-sample-account-rule");

    var accountRule = await cf.Accounts.AccessRules.CreateAsync(createAccountRuleRequest);
    logger.LogInformation("Created Account Access Rule {Id} to challenge {IP}", accountRule.Id, ipToChallenge);

    cleanupActions.Add(async () =>
    {
      logger.LogInformation("Deleting Account Access Rule: {Id}", accountRule.Id);
      await cf.Accounts.AccessRules.DeleteAsync(accountRule.Id);
      logger.LogInformation("Deleted Account Access Rule: {Id}", accountRule.Id);
    });

    return cleanupActions;
  }

  #endregion
}
