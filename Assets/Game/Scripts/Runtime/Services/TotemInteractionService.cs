public sealed class TotemInteractionService : TotemRuntimeServiceBase, ITotemRuntimeTickService, ITotemGameplaySimulationService
{
    private TotemGameFlowService flowService;
    private TotemInputService inputService;
    private TotemActorService actorService;
    private TotemMapResourceService mapResourceService;
    private TotemFirstPlayableLifecycleService lifecycleService;
    private TotemExtractionService extractionService;
    private TotemMapResourcePickup currentMapResourcePickup;
    private string currentPrompt = string.Empty;
    private string lastInteraction = string.Empty;

    public override string ServiceName => "Interaction";

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        inputService = runtime.GetService<TotemInputService>();
        actorService = runtime.GetService<TotemActorService>();
        mapResourceService = runtime.GetService<TotemMapResourceService>();
        lifecycleService = runtime.GetService<TotemFirstPlayableLifecycleService>();
        extractionService = runtime.GetService<TotemExtractionService>();
    }

    protected override void OnShutdown()
    {
        flowService = null;
        inputService = null;
        actorService = null;
        mapResourceService = null;
        lifecycleService = null;
        extractionService = null;
        currentMapResourcePickup = default;
        currentPrompt = string.Empty;
        lastInteraction = string.Empty;
    }

    public void Tick(float deltaTime)
    {
        if (flowService?.CurrentState != TotemGameFlowState.CombatHud || actorService?.Player == null)
        {
            SetCurrentFocus(default);
            return;
        }

        TotemMapResourcePickup pickup = default;
        mapResourceService?.TryFindNearest(
            actorService.Player.Position,
            TotemMapResourceService.PickupRadius,
            out pickup);
        SetCurrentFocus(pickup);

        TotemInputSnapshot input = inputService?.Current ?? TotemInputSnapshot.Empty;
        if (extractionService?.ShouldReserveLocalInteraction() == true)
        {
            SetCurrentFocus(default);
            return;
        }

        if (input.interactPressed && !(lifecycleService?.IsReviving(actorService.Player) ?? false))
        {
            TryInteractCurrent();
        }
    }

    public bool TryInteractCurrent()
    {
        return currentMapResourcePickup.IsValid && PickupMapResource(currentMapResourcePickup);
    }

    public TotemInteractionSnapshot CaptureSnapshot()
    {
        return new TotemInteractionSnapshot
        {
            hasMapResourcePickup = currentMapResourcePickup.IsValid,
            mapResourcePickupInstanceId = currentMapResourcePickup.InstanceId,
            mapResourcePickupId = currentMapResourcePickup.PickupId,
            mapResourcePickupAmount = currentMapResourcePickup.Amount,
            prompt = currentPrompt,
            lastInteraction = lastInteraction,
        };
    }

    public static string BuildMapResourcePrompt(in TotemMapResourcePickup pickup)
    {
        return pickup.IsValid ? $"F: Pick Up {pickup.ResourceId} x{pickup.Amount}" : string.Empty;
    }

    private void SetCurrentFocus(in TotemMapResourcePickup pickup)
    {
        if (currentMapResourcePickup.InstanceId == pickup.InstanceId)
        {
            return;
        }

        currentMapResourcePickup = pickup;
        currentPrompt = BuildMapResourcePrompt(currentMapResourcePickup);
        GFTrace.Info("TotemInteraction", "FocusChanged", null, GFTrace.Data(
            "mapResourceInstanceId", currentMapResourcePickup.InstanceId.ToString(),
            "prompt", currentPrompt));
    }

    private bool PickupMapResource(in TotemMapResourcePickup pickup)
    {
        mapResourceService ??= Runtime.GetService<TotemMapResourceService>();
        actorService ??= Runtime.GetService<TotemActorService>();
        if (mapResourceService == null || actorService?.Player == null
            || !mapResourceService.TryPickup(
                actorService.Player,
                pickup.InstanceId,
                out TotemMapResourcePickupResult result))
        {
            return false;
        }

        lastInteraction = $"map_resource_{result.Pickup.InstanceId}";
        RefreshFocusAfterInteraction();
        return true;
    }

    private void RefreshFocusAfterInteraction()
    {
        TotemActorModel player = actorService?.Player;
        if (player == null)
        {
            SetCurrentFocus(default);
            return;
        }

        TotemMapResourcePickup pickup = default;
        mapResourceService?.TryFindNearest(
            player.Position,
            TotemMapResourceService.PickupRadius,
            out pickup);
        SetCurrentFocus(pickup);
    }
}
