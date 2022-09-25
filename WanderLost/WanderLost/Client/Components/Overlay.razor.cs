﻿using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WanderLost.Shared.Data;
using WanderLost.Client.Services;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;

namespace WanderLost.Client.Components;

public partial class Overlay : IOverlay, IDisposable
{
  [Inject]
  private MerchantHubClient HubClient { get; set; }

  [Inject]
  private NavigationManager NavigationManager { get; set; }

  [Inject]
  private IJSRuntime JSRuntime { get; set; }

  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  public ActiveMerchantGroup? MerchantGroup;
  public Dictionary<Guid, VoteType>? Votes;
  public string? Server;
  public string? Zone;
  public string? Region;


  public bool isActive
  {
    get
    {
      return ((this.MerchantGroup is not null) && (this.Server is not null)) || (this.Zone is not null);
    }
  }

  public void ShowMap(string Zone)
  {
    this.MerchantGroup = null;
    this.Votes = null;
    this.Server = null;
    this.Region = null;
    this.Zone = Zone;
    JSRuntime.InvokeVoidAsync("HideBodyScroll");
  }

  public void ShowMerchantGroup(ActiveMerchantGroup MerchantGroup, Dictionary<Guid, VoteType> Votes, string Server, string Region)
  {
    this.MerchantGroup = MerchantGroup;
    this.Votes = Votes;
    this.Server = Server;
    this.Region= Region;
    this.Zone = null;
    JSRuntime.InvokeVoidAsync("HideBodyScroll");
  }

  public void Close()
  {
    this.MerchantGroup = null;
    this.Votes = null;
    this.Server = null;
    this.Region = null;
    this.Zone = null;
    JSRuntime.InvokeVoidAsync("ShowBodyScroll");
  }

  public async Task SubmitVote(Guid merchantId, VoteType vote)
  {
    if (Server is not null)
    {
      VoteType? existingVote = CurrentVote(merchantId);
      await HubClient.Vote(Server, merchantId, existingVote == vote ? VoteType.Unvote : vote);
    }

  }

  public VoteType? CurrentVote(Guid merchantId)
  {
    if (Votes is not null && Votes.TryGetValue(merchantId, out VoteType existingVote))
    {
      return existingVote;
    }

    return null;
  }

  protected override void OnInitialized()
  {
    NavigationManager.LocationChanged += HandleLocationChanged;
  }

  private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
  {
    Close();
  }

  public void Dispose()
  {
    NavigationManager.LocationChanged -= HandleLocationChanged;
  }
}

