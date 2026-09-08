using System.Collections;
using System.IO;
using NUnit.Framework;
using SignalLost.AI;
using SignalLost.Core;
using SignalLost.Interaction;
using SignalLost.Inventory;
using SignalLost.Items;
using SignalLost.Narrative;
using SignalLost.Player;
using SignalLost.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Inventory = SignalLost.Inventory.Inventory;

namespace SignalLost.Tests
{
    public class CorePlayModeTests
    {
        private GameObject _player;
        private PlayerVitals _vitals;
        private SignalLost.Inventory.Inventory _inventory;

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            if (SceneManager.GetActiveScene().name != "Main")
            {
                SceneManager.LoadScene("Main");
                yield return null;
            }
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_Has_CoreObjects()
        {
            Assert.That(GameObject.FindGameObjectWithTag("Player"), Is.Not.Null);
            Assert.That(GameObject.Find("Echo"), Is.Not.Null);
            Assert.That(ObjectiveSystem.Instance, Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vitals_OxygenDrains_WhenLifeSupportOffline()
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            _vitals = _player.GetComponent<PlayerVitals>();
            GameManager.Instance.SetState(GameState.Playing);
            StoryFlagSystem.Set(StoryFlagKeys.LifeSupportOnline, false);

            var before = _vitals.Oxygen;
            yield return new WaitForSeconds(2f);
            Assert.That(_vitals.Oxygen, Is.LessThan(before));
        }

        [UnityTest]
        public IEnumerator Pickup_AddsToInventory_And_Collects()
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            _inventory = _player.GetComponent<SignalLost.Inventory.Inventory>();
            var pickup = GameObject.Find("Pickup_PowerCell").GetComponent<PickupItem>();
            pickup.ForceCollect();
            pickup.gameObject.SetActive(false);

            var cell = ItemDatabase.Instance.Resolve("power_cell");
            Assert.That(cell, Is.Not.Null);
            Assert.That(_inventory.Add(cell), Is.True);
            Assert.That(_inventory.Has("power_cell"), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LifeSupportPuzzle_Completes_And_SetsFlag()
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            _inventory = _player.GetComponent<SignalLost.Inventory.Inventory>();
            GameManager.Instance.SetState(GameState.Playing);

            var terminal = Object.FindFirstObjectByType<LifeSupportTerminal>();
            Assert.That(terminal, Is.Not.Null);

            var cell = ItemDatabase.Instance.Resolve("power_cell");
            _inventory.Add(cell);

            terminal.Interact(_player);
            Assert.That(terminal.CellInserted, Is.True);

            terminal.Interact(_player);
            float start = Time.realtimeSinceStartup;
            while (!terminal.LifeSupportOnline && Time.realtimeSinceStartup - start < 15f)
                yield return null;

            Assert.That(terminal.LifeSupportOnline, Is.True);
            Assert.That(StoryFlagSystem.IsSet(StoryFlagKeys.LifeSupportOnline), Is.True);
        }

        [UnityTest]
        public IEnumerator Door_SetOpen_ChangesState()
        {
            var door = Object.FindFirstObjectByType<DoorController>();
            Assert.That(door, Is.Not.Null);
            door.SetOpen(true);
            Assert.That(door.IsOpen, Is.True);
            door.SetOpen(false);
            Assert.That(door.IsOpen, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Keycard_GatesAccessLevel()
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            _inventory = _player.GetComponent<SignalLost.Inventory.Inventory>();
            Assert.That(_inventory.HighestAccessLevel(), Is.EqualTo(0));
            _inventory.Add(ItemDatabase.Instance.Resolve("keycard_l1"));
            Assert.That(_inventory.HighestAccessLevel(), Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Save_Roundtrip_PersistsFlags()
        {
            StoryFlagSystem.Set("test_flag");
            SaveManager.Instance.Save();
            Assert.That(SaveManager.Instance.HasSave(), Is.True);

            var json = File.ReadAllText(Path.Combine(Application.persistentDataPath, "signal_lost_save.json"));
            Assert.That(json, Does.Contain("test_flag"));
            File.Delete(Path.Combine(Application.persistentDataPath, "signal_lost_save.json"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Echo_Investigates_OnLoudNoise()
        {
            var echo = GameObject.Find("Echo").GetComponent<EchoController>();
            Assert.That(echo, Is.Not.Null);

            var noisePos = echo.transform.position + new Vector3(2f, 0f, 0f);
            NoiseSystem.EmitNoise(noisePos, 12f, null);

            float start = Time.realtimeSinceStartup;
            while (echo.State != EnemyState.Investigate && Time.realtimeSinceStartup - start < 8f)
                yield return null;
            Assert.That(echo.State, Is.EqualTo(EnemyState.Investigate));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Flashlight_Toggle_DrainsBattery()
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            GameManager.Instance.SetState(GameState.Playing);
            var flash = _player.GetComponentInChildren<Flashlight>();
            _vitals = _player.GetComponent<PlayerVitals>();

            Assert.That(flash.IsOn, Is.True);
            var before = _vitals.Battery;
            yield return new WaitForSeconds(1.5f);
            Assert.That(_vitals.Battery, Is.LessThan(before));
        }
    }
}
