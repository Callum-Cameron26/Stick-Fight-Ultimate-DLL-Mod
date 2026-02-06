using System;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(StickFightUltimate.ModMain), "Stick Fight Ultimate", "6.0", "YourName")]
[assembly: MelonGame("Landfall West", "Stick Fight: The Game")]

namespace StickFightUltimate
{
    // Custom slow fade component for longer-lasting clones
    public class SlowFadeSprite : MonoBehaviour
    {
        private SpriteRenderer sprite;
        private LineRenderer line;
        private float fadeSpeed = 0.1f; // Much slower than original 2f
        
        private void Start()
        {
            this.line = base.GetComponent<LineRenderer>();
            this.sprite = base.GetComponent<SpriteRenderer>();
        }
        
        private void Update()
        {
            if (this.sprite)
            {
                this.sprite.color = new Color(this.sprite.color.r, this.sprite.color.g, this.sprite.color.b, this.sprite.color.a - Time.deltaTime * this.fadeSpeed);
                if (this.sprite.color.a < 0f)
                {
                    base.gameObject.SetActive(false);
                }
            }
            if (this.line)
            {
                this.line.material.color = new Color(this.line.material.color.r, this.line.material.color.g, this.line.material.color.b, this.line.material.color.a - Time.deltaTime * this.fadeSpeed);
                if (this.line.material.color.a < 0f)
                {
                    base.gameObject.SetActive(false);
                }
            }
        }
    }

public class ModMain : MelonMod
    {
        // Custom coroutine starter method
        private void StartModCoroutine(System.Collections.IEnumerator routine)
        {
            MelonCoroutines.Start(routine);
        }
        // All the UI stuff we need for our menu
        public bool showMenu = false;
        private Rect windowRect = new Rect(50, 50, 650, 800);
        private Vector2 scrollPosition = Vector2.zero;
        private int selectedTab = 0;
        
        // Make our UI look nice
        private GUIStyle headerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle toggleStyle;
        private GUIStyle tabStyle;
        private GUIStyle activeTabStyle;
        private bool stylesInitialized = false;
        
        // Textures for making our buttons look cool
        private Texture2D buttonNormalTexture;
        private Texture2D buttonHoverTexture;
        private Texture2D toggleTexture;
        private Texture2D tabTexture;
        private Texture2D activeTabTexture;

        // Combat cheats - the fun stuff
        public bool godMode = false;
        public bool infiniteAmmo = false;
        public bool noCooldown = false;
        
        // Keep track of when we last refilled ammo
        private float lastAmmoRefillTime = 0f;

        // Movement cheats - fly around and stuff
        public bool flyMode = false;
        public bool clickTeleport = false;
        public bool noClip = false;
        
        // Clone functionality
        public bool cloneMode = false;
        
        // Clone timing control - limit to 1 clone every 3 seconds
        private float lastCloneTime = 0f;
        private const float CLONE_COOLDOWN = 3f;
        
        // Remember how things were so we can put them back later
        private bool originalCanFly = false;
        private bool collidersStored = false;

        // Utility cheats - extra helpful stuff
        public bool instantWin = false;

        // Settings and keybinds
        public bool showNotifications = true;
        public KeyCode menuKey = KeyCode.F1;
        public KeyCode godModeKey = KeyCode.F2;

        // Internal stuff we need to keep track of
        private Controller localPlayer;
        private float lastUpdateTime = 0f;
        private const float UPDATE_INTERVAL = 0.1f;
        
        // Track instant win state for rising edge detection
        private bool lastInstantWinState = false;
        
        // Cached reflection objects for performance
        private static readonly System.Reflection.FieldInfo bulletsLeftField = typeof(Fighting).GetField("bulletsLeft", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        private static readonly System.Reflection.FieldInfo currentShotsField = typeof(Weapon).GetField("currentShots", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        
        // Store original collider states for proper restoration
        private Dictionary<Collider, bool> originalColliderStates = new Dictionary<Collider, bool>();
        private Controller storedController = null;
        private Collider[] cachedColliders = null;

        // All the tabs for our menu
        private readonly string[] tabNames = { "Combat", "Weapons", "Player", "Utility", "Clone", "Movement", "Settings" };

        public override void OnDeinitializeMelon()
        {
            // Restore gameplay state before cleanup
            RestoreOriginalState(storedController);
            
            // Clean up UI textures to prevent memory leaks
            if (buttonNormalTexture != null) UnityEngine.Object.Destroy(buttonNormalTexture);
            if (buttonHoverTexture != null) UnityEngine.Object.Destroy(buttonHoverTexture);
            if (toggleTexture != null) UnityEngine.Object.Destroy(toggleTexture);
            if (tabTexture != null) UnityEngine.Object.Destroy(tabTexture);
            if (activeTabTexture != null) UnityEngine.Object.Destroy(activeTabTexture);
            
            MelonLogger.Msg("Stick Fight Ultimate v6.0 - Cleaned up and unloaded!");
        }

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Stick Fight Ultimate v6.0 - Clean Version Loaded!");
            MelonLogger.Msg($"Menu key set to: {menuKey}");
            MelonLogger.Msg($"God Mode key set to: {godModeKey}");
            
            // Reset instant win state on initialization
            lastInstantWinState = false;
            
            // Log reflection field availability
            if (bulletsLeftField == null)
                MelonLogger.Msg("⚠ Warning: bulletsLeft field not found - infinite ammo may not work");
            if (currentShotsField == null)
                MelonLogger.Msg("⚠ Warning: currentShots field not found - infinite ammo may not work");
            
            ApplyHarmonyPatches();
        }

        public override void OnUpdate()
        {
            // Give us infinite ammo every second
            if (infiniteAmmo)
            {
                if (Time.time - lastAmmoRefillTime >= 1f)
                {
                    RefillAllAmmo();
                    lastAmmoRefillTime = Time.time;
                }
            }
            
            // Toggle the menu with our key
            if (Input.GetKeyDown(menuKey))
            {
                showMenu = !showMenu;
                if (showNotifications)
                    MelonLogger.Msg($"Menu {(showMenu ? "Opened" : "Closed")}");
            }
            
            // Quick toggle for god mode
            if (Input.GetKeyDown(godModeKey))
            {
                godMode = !godMode;
            }
            
            // NoClip automatically turns on flight - makes sense right?
            if (noClip)
            {
                // Turn on flight if noclip is on
                if (!flyMode)
                {
                    flyMode = true;
                }
            }
            
            // Hold to shoot with no cooldown
            if (noCooldown && Input.GetMouseButton(0))
            {
                var controller = GetLocalPlayer();
                if (controller != null)
                {
                    var fighting = controller.GetComponent<Fighting>();
                    if (fighting != null && fighting.weapon != null && fighting.weapon.isGun)
                    {
                        // Make the game think we can shoot again
                        if (fighting.counter > fighting.weapon.cd)
                        {
                            fighting.counter = fighting.weapon.cd + 0.001f;
                        }
                    }
                }
            }

            // Right-click teleport (only when menu is closed)
            if (clickTeleport && Input.GetMouseButtonDown(1) && !showMenu)
            {
                TeleportToMousePosition();
            }

            // Update local player reference periodically
            if (Time.time - lastUpdateTime > UPDATE_INTERVAL)
            {
                localPlayer = GetLocalPlayer();
                lastUpdateTime = Time.time;
            }

            // Clone mode - create duplicate of player (limited to 1 every 3 seconds)
            if (cloneMode && localPlayer != null)
            {
                if (Time.time - lastCloneTime >= CLONE_COOLDOWN)
                {
                    ClonePlayer();
                    lastCloneTime = Time.time;
                }
            }

            // Instant Win - trigger once when enabled (rising edge detection)
            bool currentInstantWinState = instantWin;
            
            if (currentInstantWinState && !lastInstantWinState)
            {
                InstantWin();
                instantWin = false; // Auto-disable after triggering
            }
            
            lastInstantWinState = currentInstantWinState;
            // GOD MODE - keep us alive no matter what
            if (godMode)
            {
                if (localPlayer != null)
                {
                    var health = localPlayer.GetComponent<HealthHandler>();
                    if (health != null)
                    {
                        health.health = 999999f;
                        var charInfo = localPlayer.GetComponent<CharacterInformation>();
                        if (charInfo != null)
                        {
                            charInfo.isDead = false;
                        }
                        
                        // Stop us from getting knocked around
                        var rb = localPlayer.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.velocity = Vector3.zero;
                        }
                    }
                }
            }
        }

        public override void OnGUI()
        {
            if (!showMenu) return;

            InitializeUIStylesIfNeeded();
            
            windowRect = GUILayout.Window(12345, windowRect, DrawMainWindow, "Stick Fight Ultimate v6.0");
        }

        private void InitializeUIStylesIfNeeded()
        {
            // Only set up our styles once - saves performance
            if (stylesInitialized) return;

            // Create some basic colors for our UI
            buttonNormalTexture = CreateColorTexture(new Color(0.2f, 0.4f, 0.8f, 0.8f));
            buttonHoverTexture = CreateColorTexture(new Color(0.3f, 0.6f, 1.0f, 0.9f));
            toggleTexture = CreateColorTexture(new Color(0.3f, 0.3f, 0.3f, 0.5f));
            tabTexture = CreateColorTexture(new Color(0.3f, 0.3f, 0.3f, 0.6f));
            activeTabTexture = CreateColorTexture(new Color(0.2f, 0.4f, 0.8f, 0.8f));

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.cyan }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { 
                    background = buttonNormalTexture,
                    textColor = Color.white
                },
                hover = { 
                    background = buttonHoverTexture,
                    textColor = Color.white
                },
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                normal = { 
                    background = toggleTexture,
                    textColor = Color.white
                },
                onNormal = {
                    background = toggleTexture,
                    textColor = Color.green
                },
                onHover = {
                    background = toggleTexture,
                    textColor = Color.green
                },
                fontSize = 12
            };

            tabStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { 
                    background = tabTexture,
                    textColor = Color.white
                },
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            activeTabStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { 
                    background = activeTabTexture,
                    textColor = Color.white
                },
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            stylesInitialized = true;
        }

        // Make a simple colored texture for our UI
        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            
            DrawHeader();
            DrawTabButtons();
            GUILayout.Space(15);
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            switch (selectedTab)
            {
                case 0: DrawCombatTab(); break;
                case 1: DrawWeaponsTab(); break;
                case 2: DrawPlayerTab(); break;
                case 3: DrawUtilityTab(); break;
                case 4: DrawCloneTab(); break;
                case 5: DrawMovementTab(); break;
                case 6: DrawSettingsTab(); break;
            }
            GUILayout.EndScrollView();
            
            GUILayout.Space(10);
            DrawFooter();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("🏆 STICK FIGHT ULTIMATE 🏆", headerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void DrawTabButtons()
        {
            GUILayout.BeginHorizontal();
            string[] tabIcons = { "⚔️", "🔫", "🏃", "🛠️", "👥", "🚀", "⚙️" };
            
            for (int i = 0; i < tabNames.Length; i++)
            {
                bool isSelected = selectedTab == i;
                GUI.backgroundColor = isSelected ? new Color(0.2f, 0.4f, 0.8f, 0.8f) : new Color(0.3f, 0.3f, 0.3f, 0.6f);
                
                if (GUILayout.Button($"{tabIcons[i]} {tabNames[i]}", isSelected ? activeTabStyle : tabStyle, GUILayout.Height(35)))
                {
                    selectedTab = i;
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            if (GUILayout.Button("❌ Close (F1)", buttonStyle, GUILayout.Width(120), GUILayout.Height(25)))
            {
                showMenu = false;
            }
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
        }

        private void DrawCombatTab()
        {
            GUILayout.Label("⚔️ COMBAT", headerStyle);
            GUILayout.Space(15);

            // God Mode
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = godMode ? new Color(0.2f, 0.8f, 0.2f, 0.3f) : new Color(0.3f, 0.3f, 0.3f, 0.2f);
            GUILayout.BeginVertical("box", GUILayout.Width(250));
            GUI.backgroundColor = Color.white;
            
            GUILayout.BeginHorizontal();
            godMode = GUILayout.Toggle(godMode, "", toggleStyle, GUILayout.Width(30));
            GUILayout.Label("God Mode (F2)", GUILayout.Width(100));
            GUILayout.Label(godMode ? "✅ ON" : "❌ OFF", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            
            GUILayout.Label("Complete invincibility", GUI.skin.label);
            GUILayout.Label("Health: 999999", GUI.skin.label);
            GUILayout.Label("Cannot be killed by any means", GUI.skin.label);
            
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // Infinite Ammo
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = infiniteAmmo ? new Color(0.2f, 0.8f, 0.8f, 0.3f) : new Color(0.3f, 0.3f, 0.3f, 0.2f);
            GUILayout.BeginVertical("box", GUILayout.Width(250));
            GUI.backgroundColor = Color.white;
            
            GUILayout.BeginHorizontal();
            infiniteAmmo = GUILayout.Toggle(infiniteAmmo, "", toggleStyle, GUILayout.Width(30));
            GUILayout.Label("Infinite Ammo", GUILayout.Width(100));
            GUILayout.Label(infiniteAmmo ? "∞ ON" : "❌ OFF", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            
            GUILayout.Label("Unlimited weapon ammo", GUI.skin.label);
            GUILayout.Label("Never run out of bullets", GUI.skin.label);
            
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // No Cooldown
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = noCooldown ? new Color(1f, 1f, 0.2f, 0.3f) : new Color(0.3f, 0.3f, 0.3f, 0.2f);
            GUILayout.BeginVertical("box", GUILayout.Width(250));
            GUI.backgroundColor = Color.white;
            
            GUILayout.BeginHorizontal();
            noCooldown = GUILayout.Toggle(noCooldown, "", toggleStyle, GUILayout.Width(30));
            GUILayout.Label("No Cooldown", GUILayout.Width(100));
            GUILayout.Label(noCooldown ? "⚡ ON" : "❌ OFF", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            
            GUILayout.Label("Attack without cooldowns", GUI.skin.label);
            GUILayout.Label("Rapid fire all weapons", GUI.skin.label);
            
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
        }

        private void DrawWeaponsTab()
        {
            GUILayout.Label("🔫 WEAPONS", headerStyle);
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🎲 Random Weapon", buttonStyle, GUILayout.Height(35), GUILayout.Width(180)))
            {
                SpawnRandomWeapon();
            }
            
            if (GUILayout.Button("🔥 Spawn All Weapons", buttonStyle, GUILayout.Height(35), GUILayout.Width(180)))
            {
                SpawnAllWeaponsInSky();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(20);

            GUILayout.Label("📦 SELECT WEAPON (W1-W50):", headerStyle);
            GUILayout.Space(10);

            int columns = 5;
            int maxWeapons = 50;
            bool isRowOpen = false;
            
            for (int i = 0; i < maxWeapons; i++)
            {
                if (i % columns == 0) 
                {
                    if (isRowOpen) GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    isRowOpen = true;
                }
                string weaponName = $"W{i + 1}";
                
                if (GUILayout.Button(weaponName, buttonStyle, GUILayout.Width(100), GUILayout.Height(28)))
                {
                    SpawnSpecificWeaponInSky(i);
                }

                if (i % columns == columns - 1) 
                {
                    GUILayout.EndHorizontal();
                    isRowOpen = false;
                }
            }
            
            // Close the last row if it's still open
            if (isRowOpen) 
            {
                GUILayout.EndHorizontal();
            }
        }

        private void DrawPlayerTab()
        {
            GUILayout.Label("🏃 PLAYER", headerStyle);
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = flyMode ? new Color(0.2f, 0.8f, 0.2f, 0.3f) : new Color(0.3f, 0.3f, 0.3f, 0.2f);
            GUILayout.BeginVertical("box", GUILayout.Width(250));
            GUI.backgroundColor = Color.white;
            
            GUILayout.BeginHorizontal();
            flyMode = GUILayout.Toggle(flyMode, "", toggleStyle, GUILayout.Width(30));
            GUILayout.Label("Fly Mode", GUILayout.Width(100));
            GUILayout.Label(flyMode ? "✅ ON" : "❌ OFF", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            
            GUILayout.Label("Enable flight (works alone or with noclip)", GUI.skin.label);
            GUILayout.Label("Noclip automatically enables this", GUI.skin.label);
            GUILayout.Label("Use WASD to fly freely", GUI.skin.label);
            
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = clickTeleport ? new Color(0.2f, 0.8f, 0.2f, 0.3f) : new Color(0.3f, 0.3f, 0.3f, 0.2f);
            GUILayout.BeginVertical("box", GUILayout.Width(250));
            GUI.backgroundColor = Color.white;
            
            GUILayout.BeginHorizontal();
            clickTeleport = GUILayout.Toggle(clickTeleport, "", toggleStyle, GUILayout.Width(30));
            GUILayout.Label("Click Teleport", GUILayout.Width(100));
            GUILayout.Label(clickTeleport ? "🎯 ON" : "❌ OFF", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            
            GUILayout.Label("Right click to teleport anywhere", GUI.skin.label);
            GUILayout.Label("Instant mouse position teleport", GUI.skin.label);
            
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = noClip ? new Color(0.2f, 0.8f, 0.2f, 0.3f) : new Color(0.3f, 0.3f, 0.3f, 0.2f);
            GUILayout.BeginVertical("box", GUILayout.Width(250));
            GUI.backgroundColor = Color.white;
            
            GUILayout.BeginHorizontal();
            noClip = GUILayout.Toggle(noClip, "", toggleStyle, GUILayout.Width(30));
            GUILayout.Label("No Clip", GUILayout.Width(100));
            GUILayout.Label(noClip ? "👻 ON" : "❌ OFF", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            
            GUILayout.Label("Walk through walls and objects", GUI.skin.label);
            GUILayout.Label("Automatically enables flight", GUI.skin.label);
            GUILayout.Label("Collision disabled for player", GUI.skin.label);
            
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
        }

        private void DrawUtilityTab()
        {
            GUILayout.Label("🛠️ UTILITY", headerStyle);
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("💀 Kill All Enemies", buttonStyle, GUILayout.Height(35), GUILayout.Width(180)))
            {
                KillAllEnemies();
            }
            
            if (GUILayout.Button("🔥 Spawn All Weapons", buttonStyle, GUILayout.Height(35), GUILayout.Width(180)))
            {
                SpawnAllWeaponsInSky();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            
            // Instant Win
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.8f, 0f, 0.3f);
            GUILayout.BeginVertical("box", GUILayout.Width(250));
            GUI.backgroundColor = Color.white;
            
            GUILayout.BeginHorizontal();
            instantWin = GUILayout.Toggle(instantWin, "", toggleStyle, GUILayout.Width(30));
            GUILayout.Label("Instant Win", GUILayout.Width(100));
            GUILayout.Label(instantWin ? "🏆 ON" : "❌ OFF", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            
            GUILayout.Label("Automatically win the round", GUI.skin.label);
            GUILayout.Label("Hooks game's win system", GUI.skin.label);
            
            if (GUILayout.Button("🎯 WIN NOW", buttonStyle, GUILayout.Height(30), GUILayout.Width(180)))
            {
                InstantWin();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
        }

        private void DrawCloneTab()
        {
            GUILayout.Label("👥 CLONE", headerStyle);
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = cloneMode ? new Color(0.2f, 0.8f, 0.2f, 0.3f) : new Color(0.3f, 0.3f, 0.3f, 0.2f);
            GUILayout.BeginVertical("box", GUILayout.Width(250));
            GUI.backgroundColor = Color.white;
            
            GUILayout.BeginHorizontal();
            cloneMode = GUILayout.Toggle(cloneMode, "", toggleStyle, GUILayout.Width(30));
            GUILayout.Label("Clone Mode", GUILayout.Width(100));
            GUILayout.Label(cloneMode ? "👥 ON" : "❌ OFF", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            
            GUILayout.Label("Creates long-lasting Blink Dagger style clones", GUI.skin.label);
            GUILayout.Label("Clones fade slowly over ~10 seconds", GUI.skin.label);
            GUILayout.Label("Full opacity with your exact pose and weapon", GUI.skin.label);
            
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
        }

        private void DrawMovementTab()
        {
            GUILayout.Label("🚀 MOVEMENT", headerStyle);
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = flyMode ? new Color(0.2f, 0.8f, 0.2f, 0.3f) : new Color(0.3f, 0.3f, 0.3f, 0.2f);
            GUILayout.BeginVertical("box", GUILayout.Width(250));
            GUI.backgroundColor = Color.white;
            
            GUILayout.BeginHorizontal();
            flyMode = GUILayout.Toggle(flyMode, "", toggleStyle, GUILayout.Width(30));
            GUILayout.Label("Fly Mode", GUILayout.Width(100));
            GUILayout.Label(flyMode ? "✅ ON" : "❌ OFF", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            
            GUILayout.Label("Enable flight (works alone or with noclip)", GUI.skin.label);
            GUILayout.Label("Noclip automatically enables this", GUI.skin.label);
            GUILayout.Label("Use WASD to fly freely", GUI.skin.label);
            
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = clickTeleport ? new Color(0.2f, 0.8f, 0.2f, 0.3f) : new Color(0.3f, 0.3f, 0.3f, 0.2f);
            GUILayout.BeginVertical("box", GUILayout.Width(250));
            GUI.backgroundColor = Color.white;
            
            GUILayout.BeginHorizontal();
            clickTeleport = GUILayout.Toggle(clickTeleport, "", toggleStyle, GUILayout.Width(30));
            GUILayout.Label("Click Teleport", GUILayout.Width(100));
            GUILayout.Label(clickTeleport ? "🎯 ON" : "❌ OFF", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            
            GUILayout.Label("Right click to teleport anywhere", GUI.skin.label);
            GUILayout.Label("Instant mouse position teleport", GUI.skin.label);
            
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = noClip ? new Color(0.2f, 0.8f, 0.2f, 0.3f) : new Color(0.3f, 0.3f, 0.3f, 0.2f);
            GUILayout.BeginVertical("box", GUILayout.Width(250));
            GUI.backgroundColor = Color.white;
            
            GUILayout.BeginHorizontal();
            noClip = GUILayout.Toggle(noClip, "", toggleStyle, GUILayout.Width(30));
            GUILayout.Label("No Clip", GUILayout.Width(100));
            GUILayout.Label(noClip ? "👻 ON" : "❌ OFF", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            
            GUILayout.Label("Walk through walls and objects", GUI.skin.label);
            GUILayout.Label("Automatically enables flight", GUI.skin.label);
            GUILayout.Label("Collision disabled for player", GUI.skin.label);
            
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
        }

        private void DrawSettingsTab()
        {
            GUILayout.Label("⚙️ SETTINGS", headerStyle);
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = showNotifications ? new Color(0.2f, 0.8f, 0.2f, 0.3f) : new Color(0.3f, 0.3f, 0.3f, 0.2f);
            GUILayout.BeginVertical("box", GUILayout.Width(300));
            GUI.backgroundColor = Color.white;
            
            GUILayout.BeginHorizontal();
            showNotifications = GUILayout.Toggle(showNotifications, "", toggleStyle, GUILayout.Width(30));
            GUILayout.Label("Show Notifications", GUILayout.Width(100));
            GUILayout.Label(showNotifications ? "✅ ON" : "❌ OFF", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            
            GUILayout.Label("Display cheat activation messages", GUI.skin.label);
            GUILayout.Label("Show status updates in console", GUI.skin.label);
            
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
        }

        // Get our local player - the person actually playing
        private Controller GetLocalPlayer()
        {
            if (GameManager.Instance?.playersAlive == null) return null;

            foreach (Controller player in GameManager.Instance.playersAlive)
            {
                if (player != null && !player.isAI && player.HasControl)
                {
                    return player;
                }
            }
            return null;
        }

        // Give us unlimited ammo - never run out again!
        private void RefillAllAmmo()
        {
            var controller = GetLocalPlayer();
            if (controller != null)
            {
                var fighting = controller.GetComponent<Fighting>();
                if (fighting != null)
                {
                    // Set bulletsLeft to a huge number
                    if (bulletsLeftField != null)
                    {
                        bulletsLeftField.SetValue(fighting, 99999);
                    }
                    
                    // Reset currentShots so we don't have to reload
                    if (fighting.weapon != null)
                    {
                        if (currentShotsField != null)
                        {
                            currentShotsField.SetValue(fighting.weapon, 0);
                        }
                    }
                }
            }
        }
        
        
        // Get all the enemies so we can mess with them
        private List<Controller> GetEnemyPlayers()
        {
            List<Controller> enemies = new List<Controller>();
            Controller local = GetLocalPlayer();
            if (local == null || GameManager.Instance?.playersAlive == null) return enemies;

            foreach (Controller player in GameManager.Instance.playersAlive)
            {
                if (player != null && player != local)
                {
                    enemies.Add(player);
                }
            }
            return enemies;
        }


        // Apply all our movement cheats
        private void ApplyMovementCheats()
        {
            if (localPlayer == null) return;

            // Check if local player instance changed
            if (collidersStored && localPlayer != storedController)
            {
                // Restore old controller's state before clearing caches
                RestoreOriginalState(storedController);
                
                // Clear old state and recapture for new controller
                originalColliderStates.Clear();
                cachedColliders = null;
                collidersStored = false;
                storedController = null;
            }

            // Remember how things were when we started
            if (!collidersStored)
            {
                originalCanFly = localPlayer.canFly;
                cachedColliders = localPlayer.GetComponentsInChildren<Collider>();
                foreach (var collider in cachedColliders)
                {
                    originalColliderStates[collider] = collider.enabled;
                }
                storedController = localPlayer;
                collidersStored = true;
            }

            // NoClip stuff - turn off all colliders and enable flight
            if (noClip)
            {
                // Turn on flight when noclip is on
                localPlayer.canFly = true;
                
                // Turn off all colliders so we can walk through stuff (use cached list)
                if (cachedColliders != null)
                {
                    foreach (var collider in cachedColliders)
                    {
                        if (collider != null)
                            collider.enabled = false;
                    }
                }
            }
            else
            {
                // Restore colliders to their original states (use cached list)
                if (cachedColliders != null)
                {
                    foreach (var collider in cachedColliders)
                    {
                        if (collider != null && originalColliderStates.ContainsKey(collider))
                        {
                            collider.enabled = originalColliderStates[collider];
                        }
                    }
                }
            }

            // Flight mode (only if noclip is off, since noclip already handles it)
            if (flyMode && !noClip)
            {
                localPlayer.canFly = true;
            }
            else if (!flyMode && !noClip)
            {
                // Put flight back to how it was when we started
                localPlayer.canFly = originalCanFly;
            }
        }
        
        // Restore original state for a controller (used during player change and cleanup)
        private void RestoreOriginalState(Controller controller)
        {
            if (controller == null) return;
            
            // Only restore if we have a snapshot for this specific controller
            if (storedController != controller || !collidersStored) return;
            
            // Restore original flight capability
            controller.canFly = originalCanFly;
            
            // Restore original collider states
            if (cachedColliders != null)
            {
                foreach (var collider in cachedColliders)
                {
                    if (collider != null && originalColliderStates.ContainsKey(collider))
                    {
                        collider.enabled = originalColliderStates[collider];
                    }
                }
            }
        }

        // Weapon spawning stuff
        // Give us a random weapon
        private void SpawnRandomWeapon()
        {
            if (localPlayer == null) return;

            var fighting = localPlayer.GetComponent<Fighting>();
            if (fighting == null) return;

            int weaponIndex = UnityEngine.Random.Range(0, 50);
            fighting.PickUpWeapon(weaponIndex, null);
            if (showNotifications)
                MelonLogger.Msg($"Spawned W{weaponIndex + 1} in sky");
        }

        // Spawn a specific weapon in the sky
        private void SpawnSpecificWeaponInSky(int weaponIndex)
        {
            if (localPlayer == null) return;

            var fighting = localPlayer.GetComponent<Fighting>();
            if (fighting == null) return;

            weaponIndex = Mathf.Clamp(weaponIndex, 0, 49);
            fighting.PickUpWeapon(weaponIndex, null);
            if (showNotifications)
                MelonLogger.Msg($"Spawned W{weaponIndex + 1} in sky");
        }

        // Spawn ALL the weapons - weapon paradise!
        private void SpawnAllWeaponsInSky()
        {
            if (localPlayer == null) return;

            var fighting = localPlayer.GetComponent<Fighting>();
            if (fighting == null) return;

            for (int i = 0; i < 50; i++)
            {
                fighting.PickUpWeapon(i, null);
            }

            if (showNotifications)
                MelonLogger.Msg("Spawned all weapons in the sky!");
        }

        // Kill everyone who isn't us
        private void KillAllEnemies()
        {
            var enemies = GetEnemyPlayers();
            foreach (var enemy in enemies)
            {
                var health = enemy.GetComponent<HealthHandler>();
                if (health != null)
                {
                    health.TakeDamage(9999999f, localPlayer, DamageType.Other, false, Vector3.zero, Vector3.zero);
                }
            }
            
            if (showNotifications)
                MelonLogger.Msg($"Killed {enemies.Count} enemies!");
        }

        // Teleport to where our mouse is pointing
        private void TeleportToMousePosition()
        {
            var controller = GetLocalPlayer();
            if (controller == null) return;

            if (Camera.main == null)
            {
                if (showNotifications)
                    MelonLogger.Msg("Cannot teleport: No camera found");
                return;
            }

            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            controller.transform.position = worldPos;

            if (showNotifications)
                MelonLogger.Msg("Teleported to mouse position");
        }

        // Clone player - using Blink Dagger's LeaveTrail system with slow fade
        private void ClonePlayer()
        {
            if (localPlayer == null) return;
            
            // Get the Renderers component from player (like Blink Dagger does)
            var rends = localPlayer.transform.root.GetComponentInChildren<Renderers>();
            if (rends == null) return;
            
            // Create clone using exact Blink Dagger LeaveTrail method
            var clone = UnityEngine.Object.Instantiate<GameObject>(rends.gameObject, rends.transform.position, rends.transform.rotation);
            clone.name = "BlinkClone";
            
            // Disable all MonoBehaviour components (like Blink Dagger does)
            foreach (MonoBehaviour monoBehaviour in clone.GetComponentsInChildren<MonoBehaviour>())
            {
                monoBehaviour.enabled = false;
            }
            
            // Add custom slow fade to SpriteRenderers
            foreach (SpriteRenderer spriteRenderer in clone.GetComponentsInChildren<SpriteRenderer>())
            {
                spriteRenderer.gameObject.AddComponent<SlowFadeSprite>();
            }
            
            // Add custom slow fade to LineRenderers
            foreach (LineRenderer lineRenderer in clone.GetComponentsInChildren<LineRenderer>())
            {
                lineRenderer.gameObject.AddComponent<SlowFadeSprite>();
            }
            
            // Add RemoveOnLevelChange component (like Blink Dagger does)
            clone.AddComponent<RemoveOnLevelChange>();
            
            if (showNotifications)
                MelonLogger.Msg("Long-lasting Blink Dagger clone created!");
        }

        private void InstantWin()
        {
            if (localPlayer == null) return;

            // Try to find and call the game's win method
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                // Set all enemies as dead to trigger win condition
                var enemies = GetEnemyPlayers();
                foreach (var enemy in enemies)
                {
                    var health = enemy.GetComponent<HealthHandler>();
                    if (health != null)
                    {
                        health.TakeDamage(9999999f, localPlayer, DamageType.Other, false, Vector3.zero, Vector3.zero);
                    }
                }
                
                if (showNotifications)
                    MelonLogger.Msg("Instant win activated!");
            }
        }

        // Hook into the game with Harmony patches
        private void ApplyHarmonyPatches()
        {
            int successCount = 0;
            var patchAttempts = new List<string>();
            
            try
            {
                var harmony = HarmonyInstance;
                
                // Better TakeDamage patch for god mode
                var takeDamageMethod = AccessTools.Method(typeof(HealthHandler), "TakeDamage", new Type[] 
                {
                    typeof(float), 
                    typeof(Controller), 
                    typeof(DamageType),
                    typeof(bool),
                    typeof(Vector3),
                    typeof(Vector3)
                });
                patchAttempts.Add("TakeDamage");
                if (takeDamageMethod != null)
                {
                    harmony.Patch(takeDamageMethod, new HarmonyMethod(typeof(ModMain).GetMethod("TakeDamage_Combined")));
                    MelonLogger.Msg($"✓ TakeDamage patch applied: {takeDamageMethod.DeclaringType.Name}.{takeDamageMethod.Name}");
                    successCount++;
                }
                else
                {
                    MelonLogger.Msg("✗ Failed to find TakeDamage method");
                }

                // Stop us from dying
                var dieMethod = AccessTools.Method(typeof(HealthHandler), "Die");
                patchAttempts.Add("Die");
                if (dieMethod != null)
                {
                    harmony.Patch(dieMethod, new HarmonyMethod(typeof(ModMain).GetMethod("Die_Prefix")));
                    MelonLogger.Msg($"✓ Die patch applied: {dieMethod.DeclaringType.Name}.{dieMethod.Name}");
                    successCount++;
                }
                else
                {
                    MelonLogger.Msg("✗ Failed to find Die method");
                }

                // Infinite ammo patch for shooting
                var shootMethod = AccessTools.Method(typeof(Weapon), "ActuallyShoot");
                patchAttempts.Add("ActuallyShoot");
                if (shootMethod != null)
                {
                    harmony.Patch(shootMethod, new HarmonyMethod(typeof(ModMain).GetMethod("ActuallyShoot_Prefix")));
                    MelonLogger.Msg($"✓ ActuallyShoot patch applied: {shootMethod.DeclaringType.Name}.{shootMethod.Name}");
                    successCount++;
                }
                else
                {
                    MelonLogger.Msg("✗ Failed to find ActuallyShoot method");
                }

                // No cooldown patch for attacking
                var attackMethod = AccessTools.Method(typeof(Fighting), "Attack");
                patchAttempts.Add("Attack");
                if (attackMethod != null)
                {
                    harmony.Patch(attackMethod, new HarmonyMethod(typeof(ModMain).GetMethod("Attack_Prefix")));
                    MelonLogger.Msg($"✓ Attack patch applied: {attackMethod.DeclaringType.Name}.{attackMethod.Name}");
                    successCount++;
                }
                else
                {
                    MelonLogger.Msg("✗ Failed to find Attack method");
                }

                // Stop weapon throwing for infinite ammo
                var throwWeaponMethod = AccessTools.Method(typeof(Fighting), "ThrowWeapon", new Type[] { typeof(bool) });
                patchAttempts.Add("ThrowWeapon");
                if (throwWeaponMethod != null)
                {
                    harmony.Patch(throwWeaponMethod, new HarmonyMethod(typeof(ModMain).GetMethod("ThrowWeapon_Prefix")));
                    MelonLogger.Msg($"✓ ThrowWeapon patch applied: {throwWeaponMethod.DeclaringType.Name}.{throwWeaponMethod.Name}");
                    successCount++;
                }
                else
                {
                    MelonLogger.Msg("✗ Failed to find ThrowWeapon method");
                }

                // Network weapon throwing prevention for infinite ammo
                var networkThrowWeaponMethod = AccessTools.Method(typeof(Fighting), "NetworkThrowWeapon", new Type[] { typeof(bool), typeof(byte), typeof(Vector3), typeof(Vector3), typeof(ushort), typeof(ushort) });
                patchAttempts.Add("NetworkThrowWeapon");
                if (networkThrowWeaponMethod != null)
                {
                    harmony.Patch(networkThrowWeaponMethod, new HarmonyMethod(typeof(ModMain).GetMethod(nameof(NetworkThrowWeapon_Prefix))));
                    MelonLogger.Msg($"✓ NetworkThrowWeapon patch applied: {networkThrowWeaponMethod.DeclaringType.Name}.{networkThrowWeaponMethod.Name}");
                    successCount++;
                }
                else
                {
                    MelonLogger.Msg("✗ Failed to find NetworkThrowWeapon method");
                }

                MelonLogger.Msg($"Harmony patches: {successCount}/{patchAttempts.Count} applied successfully!");
            }
            catch (Exception ex)
            {
                MelonLogger.Msg($"Harmony patch error: {ex.Message}");
            }
        }

            // Handle damage for god mode
            [HarmonyPrefix]
            public static bool TakeDamage_Combined(HealthHandler __instance, float damage, Controller damager, DamageType dmgType = DamageType.Other, bool playParticles = false, Vector3 position = default(Vector3), Vector3 direction = default(Vector3))
            {
                var main = Melon<ModMain>.Instance;
                if (main == null) return true;

                var controller = __instance.GetComponent<Controller>();
                var localController = main.GetLocalPlayer();

                // God Mode - we can't die
                if (main.godMode && controller == localController)
                {
                    __instance.health = 999999f;
                    var charInfo = controller.GetComponent<CharacterInformation>();
                    if (charInfo != null)
                    {
                        charInfo.isDead = false;
                    }
                    return false;
                }

                return true;
            }

        // Stop us from dying completely
        [HarmonyPrefix]
        public static bool Die_Prefix(HealthHandler __instance)
        {
            var main = Melon<ModMain>.Instance;
            if (main == null) return true;

            var controller = __instance.GetComponent<Controller>();
            var localController = main.GetLocalPlayer();

            // God Mode - death is not allowed
            if (main.godMode && controller == localController)
            {
                __instance.health = 999999f;
                var charInfo = controller.GetComponent<CharacterInformation>();
                if (charInfo != null)
                {
                    charInfo.isDead = false;
                }
                return false;
            }

            return true;
        }

        // Infinite ammo - never run out of bullets
        [HarmonyPrefix]
        public static bool ActuallyShoot_Prefix(Weapon __instance)
        {
            var main = Melon<ModMain>.Instance;
            if (main == null) return true;

            // Infinite Ammo - keep our ammo full
            if (main.infiniteAmmo)
            {
                // Reset shots so we don't have to reload
                if (currentShotsField != null)
                {
                    currentShotsField.SetValue(__instance, 0);
                }
                // Make sure we never run out of bullets
                if (bulletsLeftField != null)
                {
                    var fighting = __instance.GetComponent<Fighting>();
                    if (fighting != null)
                    {
                        bulletsLeftField.SetValue(fighting, 99999);
                    }
                }
            }

            return true;
        }

        // Stop throwing weapons when we have infinite ammo
        [HarmonyPrefix]
        public static bool ThrowWeapon_Prefix(Fighting __instance, bool justDrop)
        {
            var main = Melon<ModMain>.Instance;
            if (main == null) return true;

            // Don't throw weapons if we have infinite ammo (unless we're just dropping)
            if (main.infiniteAmmo && !justDrop)
            {
                // Reset bullets to stop the throwing
                if (bulletsLeftField != null)
                {
                    bulletsLeftField.SetValue(__instance, 99999);
                }
                return false;
            }

            return true;
        }

        // Stop network weapon throwing when we have infinite ammo
        [HarmonyPrefix]
        public static bool NetworkThrowWeapon_Prefix(Fighting __instance, bool justDrop, byte weaponIndex, Vector3 position, Vector3 rotation, ushort spawnIndex, ushort syncIndex)
        {
            var main = Melon<ModMain>.Instance;
            if (main == null) return true;

            // Don't throw weapons online if we have infinite ammo (unless we're just dropping)
            if (main.infiniteAmmo && !justDrop)
            {
                // Reset bullets to stop the throwing
                if (bulletsLeftField != null)
                {
                    bulletsLeftField.SetValue(__instance, 99999);
                }
                return false;
            }

            return true;
        }

        // No cooldown - attack as fast as we want
        [HarmonyPrefix]
        public static bool Attack_Prefix(Fighting __instance)
        {
            var main = Melon<ModMain>.Instance;
            if (main == null) return true;

            // No Cooldown - skip the cooldown wait
            if (main.noCooldown && __instance.weapon != null)
            {
                // Make weapon cooldown almost zero for this attack
                var originalCd = __instance.weapon.cd;
                __instance.weapon.cd = 0.00001f;
                
                // Put the original cooldown back after we attack
                MelonCoroutines.Start(RestoreCooldown(__instance, originalCd));
            }

            return true;
        }
        
        // Put the weapon cooldown back to normal
        private static System.Collections.IEnumerator RestoreCooldown(Fighting fighting, float originalCd)
        {
            yield return new WaitForSeconds(0.01f);
            if (fighting != null && fighting.weapon != null)
            {
                fighting.weapon.cd = originalCd;
            }
        }
    }
}
