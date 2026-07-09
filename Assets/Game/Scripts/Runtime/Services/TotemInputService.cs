using UnityEngine;

public interface ITotemInputProvider
{
    float UnscaledTime { get; }
    Vector3 MousePosition { get; }
    bool GetKey(KeyCode keyCode);
    bool GetKeyDown(KeyCode keyCode);
    bool GetMouseButton(int button);
    bool GetMouseButtonDown(int button);
}

public sealed class TotemUnityInputProvider : ITotemInputProvider
{
    public static readonly TotemUnityInputProvider Instance = new TotemUnityInputProvider();

    private TotemUnityInputProvider()
    {
    }

    public float UnscaledTime => Time.unscaledTime;

    public Vector3 MousePosition => Input.mousePosition;

    public bool GetKey(KeyCode keyCode)
    {
        return Input.GetKey(keyCode);
    }

    public bool GetKeyDown(KeyCode keyCode)
    {
        return Input.GetKeyDown(keyCode);
    }

    public bool GetMouseButton(int button)
    {
        return Input.GetMouseButton(button);
    }

    public bool GetMouseButtonDown(int button)
    {
        return Input.GetMouseButtonDown(button);
    }
}

public sealed class TotemInputService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    private float attackHoldStartTime = -1f;
    private bool attackWasHolding;
    private ITotemInputProvider inputProvider = TotemUnityInputProvider.Instance;

    public override string ServiceName => "Input";

    public TotemInputSnapshot Current { get; private set; } = TotemInputSnapshot.Empty;

    public ITotemInputProvider InputProvider => inputProvider;

    public void SetInputProvider(ITotemInputProvider provider)
    {
        inputProvider = provider ?? TotemUnityInputProvider.Instance;
        ResetHoldState();
        Current = TotemInputSnapshot.Empty;
    }

    public void Tick(float deltaTime)
    {
        Current = ReadInputSnapshot();
    }

    public TotemInputSnapshot ReadInputSnapshot()
    {
        bool attackHeld = inputProvider.GetMouseButton(0);
        UpdateAttackHold(attackHeld);

        var move = ReadMove(inputProvider);
        bool hasAimWorldPoint = TryProjectMouseToGround(inputProvider.MousePosition, out var aimWorldPoint);
        bool skillSlotEPressed = inputProvider.GetKeyDown(KeyCode.E);
        bool skillSlotQPressed = inputProvider.GetKeyDown(KeyCode.Q);
        return new TotemInputSnapshot
        {
            move = move,
            hasAimWorldPoint = hasAimWorldPoint,
            aimWorldPoint = aimWorldPoint,
            attackPressed = inputProvider.GetMouseButtonDown(0),
            attackHeld = attackHeld,
            attackHoldDuration = GetAttackHoldDuration(),
            skillPressed = skillSlotEPressed,
            skillSlotEPressed = skillSlotEPressed,
            skillSlotQPressed = skillSlotQPressed,
            dodgePressed = inputProvider.GetKeyDown(KeyCode.Space),
            interactPressed = inputProvider.GetKeyDown(KeyCode.F),
            escapePressed = inputProvider.GetKeyDown(KeyCode.Escape),
            selfTattooTogglePressed = inputProvider.GetKeyDown(KeyCode.Tab),
        };
    }

    public static Vector2 NormalizeMove(float x, float y)
    {
        var move = new Vector2(x, y);
        return move.sqrMagnitude > 1f ? move.normalized : move;
    }

    private static Vector2 ReadMove(ITotemInputProvider provider)
    {
        float x = 0f;
        float y = 0f;
        if (provider.GetKey(KeyCode.A) || provider.GetKey(KeyCode.LeftArrow))
        {
            x -= 1f;
        }

        if (provider.GetKey(KeyCode.D) || provider.GetKey(KeyCode.RightArrow))
        {
            x += 1f;
        }

        if (provider.GetKey(KeyCode.W) || provider.GetKey(KeyCode.UpArrow))
        {
            y += 1f;
        }

        if (provider.GetKey(KeyCode.S) || provider.GetKey(KeyCode.DownArrow))
        {
            y -= 1f;
        }

        return NormalizeMove(x, y);
    }

    public static bool TryProjectMouseToGround(Vector3 mousePosition, out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        if (!IsFinite(mousePosition.x) || !IsFinite(mousePosition.y) || !IsFinite(mousePosition.z))
        {
            return false;
        }

        var camera = Camera.main;
        if (camera == null)
        {
            return false;
        }

        var ray = camera.ScreenPointToRay(mousePosition);
        var plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float enter))
        {
            worldPoint = ray.GetPoint(enter);
            return true;
        }

        worldPoint = ray.GetPoint(50f);
        return true;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private float GetAttackHoldDuration()
    {
        if (!attackWasHolding || attackHoldStartTime < 0f)
        {
            return 0f;
        }

        return inputProvider.UnscaledTime - attackHoldStartTime;
    }

    private void UpdateAttackHold(bool currentlyHeld)
    {
        if (currentlyHeld && !attackWasHolding)
        {
            attackHoldStartTime = inputProvider.UnscaledTime;
        }
        else if (!currentlyHeld)
        {
            attackHoldStartTime = -1f;
        }

        attackWasHolding = currentlyHeld;
    }

    private void ResetHoldState()
    {
        attackHoldStartTime = -1f;
        attackWasHolding = false;
    }
}
