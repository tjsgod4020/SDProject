using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SDProject.Combat.Board;

namespace SDProject.Combat
{
    /// <summary>
    /// Binds a unit to a board slot at runtime.
    /// - Exposes read-only properties Team/Index for external usage (binder.Team / binder.Index).
    /// - If Index < 0, finds the nearest slot of the given Team and snaps to it.
    /// - If BoardRuntime exists, tries to register the unit there (via direct ref or reflection fallback).
    /// - If BoardRuntime is missing, still snaps to the slot's mount for safe v1 usage.
    /// </summary>
    [DisallowMultipleComponent]
    public class SimpleUnitBinder : MonoBehaviour
    {
        [Header("Binding")]
        [SerializeField] private TeamSide team = TeamSide.Ally;
        [SerializeField] private int index = -1; // -1 = auto (nearest)
        [Tooltip("Wait one frame before binding to ensure BoardLayout/Runtime are ready.")]
        [SerializeField] private bool waitOneFrame = true;

        [Header("Snap")]
        [Tooltip("If true, snaps the transform to slot mount after binding.")]
        [SerializeField] private bool snapToMount = true;

        // ---- Public read-only properties (권장안 핵심) ----
        public TeamSide Team => team;
        public int Index => index;

        // ---- Runtime references (optional/diagnostics) ----
        public CharacterSlot CurrentSlot { get; private set; }
        public MonoBehaviour BoardRuntimeRef { get; private set; } // keep as MonoBehaviour to avoid hard dependency
        private Component _boardRuntimeComp; // cached component (any type named "BoardRuntime")
        private Transform _cachedMount;
        private Transform _t;

        private void Awake()
        {
            _t = transform;
        }

        private void OnEnable()
        {
            if (waitOneFrame) StartCoroutine(CoBindNextFrame());
            else TryBindNow();
        }

        private IEnumerator CoBindNextFrame()
        {
            yield return null; // let BoardLayout create slots & BoardRuntime scan them
            TryBindNow();
        }

        /// <summary>
        /// External setter with optional rebind.
        /// </summary>
        public void SetBinding(TeamSide newTeam, int newIndex, bool rebind = true)
        {
            team = newTeam;
            index = newIndex;
            if (rebind && isActiveAndEnabled)
            {
                TryBindNow();
            }
        }

        /// <summary>
        /// Re-run binding with current team/index.
        /// </summary>
        [ContextMenu("Rebind")]
        public void Rebind()
        {
            TryBindNow();
        }

        private void TryBindNow()
        {
            // 1) Find BoardRuntime (optional)
            _boardRuntimeComp = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(mb => mb != null && mb.GetType().Name == "BoardRuntime");
            BoardRuntimeRef = _boardRuntimeComp as MonoBehaviour;

            // 2) Resolve target slot index
            var targetIndex = index >= 0 ? index : FindNearestSlotIndex(team);

            if (targetIndex < 0)
            {
                Debug.LogWarning($"[SimpleUnitBinder] No valid slot found for Team={team}.", this);
                return;
            }

            index = targetIndex; // lock-in chosen index

            // 3) Get the CharacterSlot we are going to occupy/snap to
            var slot = GetSlotByTeamIndex(team, targetIndex);
            if (slot == null)
            {
                Debug.LogWarning($"[SimpleUnitBinder] Slot not found Team={team}, Index={targetIndex}.", this);
                return;
            }

            CurrentSlot = slot;
            _cachedMount = slot.mount != null ? slot.mount : slot.transform;

            // 4) Register to BoardRuntime if available; otherwise just snap
            bool registered = TryRegisterToBoardRuntime(_boardRuntimeComp, team, targetIndex);

            if (!registered)
            {
                // Fallback: just snap to mount so v1 keeps working
                if (snapToMount && _cachedMount)
                {
                    _t.position = _cachedMount.position;
                    _t.rotation = _cachedMount.rotation;
                }
                Debug.Log($"[SimpleUnitBinder] Fallback snap only. (No BoardRuntime or method not found) Team={team}, Index={index}, Unit={name}", this);
            }
            else
            {
                if (snapToMount && _cachedMount)
                {
                    _t.position = _cachedMount.position;
                    _t.rotation = _cachedMount.rotation;
                }
                Debug.Log($"[SimpleUnitBinder] Registered to BoardRuntime. Team={team}, Index={index}, Unit={name}", this);
            }
        }

        /// <summary>
        /// Finds nearest slot index by distance to this transform among the given team.
        /// Returns -1 if none found.
        /// </summary>
        private int FindNearestSlotIndex(TeamSide side)
        {
            var allSlots = FindObjectsByType<CharacterSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(s => s != null && s.Team == side)
                .ToList();

            if (allSlots.Count == 0) return -1;

            var nearest = allSlots
                .OrderBy(s => (s.transform.position - _t.position).sqrMagnitude)
                .First();

            return nearest.Index;
        }

        /// <summary>
        /// Fetch slot by (team, index) using scene scan. Returns null if not found.
        /// </summary>
        private CharacterSlot GetSlotByTeamIndex(TeamSide side, int idx)
        {
            return FindObjectsByType<CharacterSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(s => s != null && s.Team == side && s.Index == idx);
        }

        /// <summary>
        /// Attempts to call BoardRuntime.RegisterUnit(this.gameObject, team, index) or RegisterUnit(SimpleUnit, TeamSide, int)
        /// via reflection so we don't hard depend on a specific signature.
        /// Returns true if any registration method was successfully invoked.
        /// </summary>
        private bool TryRegisterToBoardRuntime(Component boardRuntime, TeamSide side, int idx)
        {
            if (boardRuntime == null) return false;

            var brType = boardRuntime.GetType();

            // Try some common signatures by reflection (no hard dependency):
            // 1) RegisterUnit(GameObject, TeamSide, int)
            // 2) RegisterUnit(Component/MonoBehaviour, TeamSide, int)
            // 3) RegisterUnit(object, TeamSide, int)
            // 4) RegisterUnit(TeamSide, int, GameObject)

            var args1 = new object[] { this.gameObject, side, idx };
            var args2 = new object[] { this, side, idx };

            // (a) exact by name with 3 params
            var mi = brType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "RegisterUnit") return false;
                    var ps = m.GetParameters();
                    if (ps.Length != 3) return false;
                    return true;
                });

            if (mi != null)
            {
                try
                {
                    // try with (GameObject, TeamSide, int)
                    var parameters = mi.GetParameters();
                    object[] useArgs = null;

                    // Heuristic match
                    if (parameters[0].ParameterType == typeof(GameObject))
                        useArgs = args1;
                    else if (parameters[0].ParameterType.IsAssignableFrom(this.GetType()))
                        useArgs = args2;
                    else
                        useArgs = args1; // last resort

                    mi.Invoke(boardRuntime, useArgs);
                    return true;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[SimpleUnitBinder] RegisterUnit reflection failed: {ex.Message}", this);
                }
            }

            // (b) alternative signature: RegisterUnit(TeamSide, int, GameObject)
            mi = brType.GetMethod("RegisterUnit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                   null, new[] { typeof(TeamSide), typeof(int), typeof(GameObject) }, null);
            if (mi != null)
            {
                try
                {
                    mi.Invoke(boardRuntime, new object[] { side, idx, this.gameObject });
                    return true;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[SimpleUnitBinder] RegisterUnit(TeamSide,int,GameObject) failed: {ex.Message}", this);
                }
            }

            return false;
        }

        // Optional: clean-up hook – try to unregister if method exists
        private void OnDisable()
        {
            TryUnregister();
        }

        private void OnDestroy()
        {
            TryUnregister();
        }

        private void TryUnregister()
        {
            if (_boardRuntimeComp == null) return;

            var brType = _boardRuntimeComp.GetType();
            var mi = brType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "UnregisterUnit") return false;
                    var ps = m.GetParameters();
                    return ps.Length == 1; // try UnregisterUnit(GameObject) or (Component)
                });

            if (mi != null)
            {
                try
                {
                    mi.Invoke(_boardRuntimeComp, new object[] { this.gameObject });
                }
                catch { /* ignore */ }
            }
        }
    }
}