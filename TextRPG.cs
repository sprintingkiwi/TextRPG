using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text.Json;
using System.IO;

namespace TextRPG
{
    #region Items
    ////////////////////////////////////////////////
    //ITEMS
    ////////////////////////////////////////////////
    public abstract class Item
    {
        public abstract string Name { get; }
        public abstract int Value { get; }
    }

    public abstract class Equipment : Item
    {
        public abstract Character.EquipSlot RequiredSlot { get; }
        public virtual int Speed { get; }

        public virtual void Equip(Character character)
        {
            character.equipment.Add(RequiredSlot, this);
            character.Speed.SetModifier(Speed, this);
        }

        public virtual void Unequip(Character character)
        {
            character.equipment.Remove(RequiredSlot);
            foreach (Stat stat in character.stats)
                stat.RemoveModifierFromSource(this);
        }
    }

    public abstract class Weapon : Equipment
    {
        public enum DamageType { Slashing, Bludgeoning, Piercing, None }

        public abstract DamageType Damage { get; } // Type of damage (Slashing, Piercing, ecc.)
        public abstract DamageType Weakness { get; }
        public abstract int Attack { get; }
        public virtual int RndRange { get; set; } // Amount of random in damage
        public virtual int Precision { get; set; } // Chances of hitting the target (%)
        //public int ap = 1; // Action Points required to use this weapon

        public override Character.EquipSlot RequiredSlot => Character.EquipSlot.Weapon;

        public override void Equip(Character character)
        {
            base.Equip(character);
            character.Attack.SetModifier(Attack, this);
        }
    }

    public class NaturalWeapon : Weapon
    {
        public override DamageType Damage => DamageType.None;
        public override DamageType Weakness => DamageType.None;
        public override string Name => "";
        public override int Value => 0;
        public override int Attack => 0;
        public override int RndRange { get; set; }
        public override int Precision { get; set; }
        public override int Speed => 0;

        public NaturalWeapon(int rndRange, int precision)
        {
            RndRange = rndRange;
            Precision = precision;
        }
    }

    public abstract class Sword : Weapon
    {
        public override DamageType Damage => DamageType.Slashing;
        public override DamageType Weakness => DamageType.Piercing;
        public override int RndRange => 10;
        public override int Precision => 90;
        public override int Speed => 0;
    }

    public abstract class Spear : Weapon
    {
        public override DamageType Damage => DamageType.Piercing;
        public override DamageType Weakness => DamageType.Bludgeoning;
        public override int RndRange => 20;
        public override int Precision => 85;
        public override int Speed => -5;
    }

    public abstract class Hammer : Weapon
    {
        public override DamageType Damage => DamageType.Bludgeoning;
        public override DamageType Weakness => DamageType.Slashing;
        public override int RndRange => 30;
        public override int Precision => 80;
        public override int Speed => -10;
    }

    public abstract class Armor : Equipment
    {
        public abstract int Defense { get; }

        public override void Equip(Character character)
        {
            base.Equip(character);

            character.Defense.SetModifier(Defense, this);
        }
    }

    public abstract class BodyArmor : Armor
    {
        public override Character.EquipSlot RequiredSlot => Character.EquipSlot.Body;
    }

    public abstract class Shield : Armor
    {
        public override Character.EquipSlot RequiredSlot => Character.EquipSlot.Shield;
    }

    public abstract class Accessory : Equipment
    {
        public override Character.EquipSlot RequiredSlot => Character.EquipSlot.Accessory;
    }
    #endregion


    #region Spells
    ////////////////////////////////////////////////
    //SPELLS
    ////////////////////////////////////////////////
    public abstract class Spell
    {
        public enum ElementType { Earth, Water, Fire, Air, None }

        public abstract string Name { get; }
        public abstract int Damage { get; }
        public abstract ElementType Element { get; }
    }
    #endregion


    #region Characters
    ////////////////////////////////////////////////
    // CHARACTERS
    ////////////////////////////////////////////////
    public class Stat
    {
        private int baseValue;
        public Dictionary<Equipment, int> modifiers = new Dictionary<Equipment, int>();

        public void SetBaseValue(int baseValue)
        {
            this.baseValue = baseValue;
        }

        public int Value
        {
            get
            {
                int finalValue = baseValue;
                foreach (KeyValuePair<Equipment, int> mod in modifiers)
                    finalValue += mod.Value;
                return finalValue;
            }
        }

        public void SetModifier(int mod, Equipment source)
        {
            if (modifiers.ContainsKey(source))
                modifiers[source] = mod;
            else
                modifiers.Add(source, mod);
        }

        public void RemoveModifierFromSource(Equipment source)
        {
            modifiers.Remove(source);
        }
    }

    public abstract class Character
    {
        public static int BaseUnarmedCombatDamage = 10;

        //public int level = 1;
        protected int damage; // Amount of damage taken so far

        public abstract string Name { get; } // Total hit points
        public Stat MaxHP { get; } = new Stat(); // Total hit points
        public Stat Attack { get; } = new Stat();
        public Stat Defense { get; } = new Stat();
        public Stat Speed { get; } = new Stat();
        public Stat[] stats;
        public abstract Spell.ElementType ElementWeakness { get; }
        public abstract Spell.ElementType ElementResistance { get; }
        public enum EquipSlot { Head, Body, Shield, Weapon, Accessory }
        public readonly Dictionary<EquipSlot, Equipment> equipment = new Dictionary<EquipSlot, Equipment>(); // Dictionary from slot name to item class instance
        protected List<Spell> spellbook = new List<Spell>(); // Known spells
        public List<Spell> Spellbook { get => spellbook; }
        protected NaturalWeapon NaturalWeapon { get; set; } = new NaturalWeapon(15, 90);
        public int CurrentHP
        {
            get
            {
                int hp = MaxHP.Value - damage;
                if (hp < 0) { hp = 0; } // Prevent negative Hit Points
                return hp;
            }
            set
            {
                if (value < 0) value = 0; // Prevent negative Hit Points
                damage = MaxHP.Value - value;
            }
        }

        public Character() // Characters initialization
        {
            stats = new Stat[] { Attack, Defense, Speed };
        }

        protected abstract string[] GetBattleActions();

        public abstract void ChooseBattleAction(Character opponent);

        protected virtual void ExecuteBattleAction(int actionID, Character opponent)
        {
            switch (GetBattleActions()[actionID])
            {
                case "Attack":
                    WeaponAttack(opponent);
                    break;
                case "Cast Spell":
                    ChooseSpell(opponent);
                    break;
                case "Drink Potion":
                    DrinkPotion();
                    break;
                default:
                    break;
            }
        }

        public string ShowStatus()
        {
            return Name + " - HP: " + CurrentHP.ToString();
        }

        protected abstract string LogAction(string actionName);

        public virtual void TakeDamage(int damage)
        {
            if (damage < 1) { damage = 1; } // Prevent negative or null damage values
            this.damage += damage;
            Utils.Tale(LogAction("receive") + " " + damage.ToString() + " points of damage");
            if (this.damage > MaxHP.Value) { this.damage = MaxHP.Value; } // Prevent damage bigger than MaxHP
        }

        protected void WeaponAttack(Character target)
        {
            Weapon weapon = equipment[EquipSlot.Weapon] as Weapon;
            Weapon targetWeapon = target.equipment[EquipSlot.Weapon] as Weapon;

            string attackLog = LogAction("attack") + " " + target.Name;
            if (!(weapon is NaturalWeapon)) { attackLog += " with " + weapon.Name; }
            Utils.Tale(attackLog);

            Random rnd = new Random();
            int hitRoll = rnd.Next(1, 100);
            bool hit = hitRoll <= weapon.Precision - target.Speed.Value;
            bool critical = hitRoll > 90;
            if (hit)
            {
                int attackValue = rnd.Next(Attack.Value - weapon.RndRange, Attack.Value + weapon.RndRange + 1);
                int damage = attackValue - target.Defense.Value;
                if (targetWeapon.Weakness == weapon.Damage)
                {
                    damage = (int)(damage * 1.5f); // Apply weapon weakness
                    Utils.Tale("Weapon Advantage!");
                }
                if (critical) { damage *= 3; Utils.Tale("CRITICAL HIT!"); } // Apply critical bonus
                target.TakeDamage(damage);
            }
            else { Utils.Tale("The attack misses..."); }
        }

        public virtual void LearnSpell(Spell spell)
        {
            Utils.Tale("Learned spell: " + spell.Name);
            spellbook.Add(spell);
        }

        protected abstract void ChooseSpell(Character target);

        protected virtual void CastSpell(Spell spell, Character target)
        {
            // Cast spell
            Utils.Tale(LogAction("cast") + " the spell: " + spell.Name);
            int damage = spell.Damage;
            if (target.ElementWeakness == spell.Element) // Apply element weakness
            {
                damage *= 2;
                Utils.Tale("Element Weakness!");
            }
            else if (target.ElementResistance == spell.Element)
            {
                damage /= 2;
                Utils.Tale("Element Resistance!");
            }
            target.TakeDamage(damage);
        }

        protected void DrinkPotion()
        {
            Utils.Tale(LogAction("drink") + " a Potion");
            damage = 0;
            Utils.Tale(Name + " HP: " + CurrentHP);
        }

        enum BattleOutcome { Continue, Win, Lose }

        public Character Battle(Enemy enemy)
        {
            Utils.Tale("Starting battle with " + enemy.Name);

            BattleOutcome outcome = BattleOutcome.Continue;
            BattleOutcome CheckOutcome()
            {
                if (CurrentHP <= 0) { return BattleOutcome.Lose; }
                else if (enemy.CurrentHP <= 0) { return BattleOutcome.Win; }
                else { return BattleOutcome.Continue; }
            }

            // Fight
            bool playerTurn = Speed.Value >= enemy.Speed.Value;
            while (outcome == BattleOutcome.Continue)
            {
                if (playerTurn)
                {
                    ChooseBattleAction(enemy);
                    Utils.Tale(enemy.ShowStatus());
                    outcome = CheckOutcome();
                }
                else
                {
                    enemy.WeaponAttack(this);
                    Utils.Tale(ShowStatus());
                    outcome = CheckOutcome();
                }
                playerTurn = !playerTurn;
            }

            // Outcome
            Console.WriteLine("Battle ended");
            if (outcome == BattleOutcome.Win)
            {
                return this;
            }
            else
            {
                return enemy;
            }
        }
    }

    public class Player : Character
    {
        public override string Name => "You";
        public override Spell.ElementType ElementWeakness => Spell.ElementType.None;
        public override Spell.ElementType ElementResistance => Spell.ElementType.None;
        protected List<Item> Inventory { get; set; } = new List<Item>();

        public Player()
        {
            MaxHP.SetBaseValue(50);
            Attack.SetBaseValue(10);
            Defense.SetBaseValue(5);
            Speed.SetBaseValue(5);
        }

        protected override string LogAction(string actionName)
        {
            return "You " + actionName;
        }

        public override void TakeDamage(int damage)
        {
            base.TakeDamage(damage);

            // END GAME IF YOU DIE
            if (CurrentHP <= 0)
            {
                Utils.Tale("You died.");
                //GameController.gameOver = true;
            }
        }

        protected override string[] GetBattleActions()
        {
            string[] availableActions = new string[]
            {
                "Attack",
                "Cast Spell",
                "Drink Potion",
                "Change Weapon"
            };

            return availableActions.ToArray();
        }

        public override void ChooseBattleAction(Character opponent) // Get player choice on fight
        {
            // Ask the player to choose among available actions
            ExecuteBattleAction(Utils.ProcessChoice(GetBattleActions()), opponent);
        }

        protected override void ExecuteBattleAction(int actionID, Character opponent)
        {
            base.ExecuteBattleAction(actionID, opponent);

            switch (GetBattleActions()[actionID])
            {
                case "Change Weapon":
                    ChangeEquipment(ChooseItemFromList<Equipment>(GetEquipmentFromInventory(EquipSlot.Weapon)));
                    break;
                default:
                    break;
            }
        }

        protected override void ChooseSpell(Character target)
        {
            Utils.Tale("Bind the spell to its name:\n", stop: false);
            string spellName = Console.ReadLine();

            // Check if spell is in spellbook
            Spell spell = null;
            foreach (Spell s in spellbook)
                if (s.Name == spellName) { spell = s; break; }
            if (spell == null)
            {
                Utils.Tale("You fail to cast the spell...");
                return;
            }

            CastSpell(spell, target);
        }

        public void AddToInventory(Item item)
        {
            if (Inventory.Count <= 10)
            {
                Inventory.Add(item);
                Utils.Tale(item.Name + " added to the inventory.");
            }
            else
            {
                Utils.Tale("You cannot take " + item.Name + ": inventory is full.");
            }
        }

        public void RemoveFromInventory(Item item)
        {
            Inventory.Remove(item);
        }

        public List<Item> GetItemsFromInventory<itemType>() where itemType : Item
        {
            // Get list of EquipType type of items in inventory
            List<itemType> availableEquips = new List<itemType>();
            foreach (Item item in Inventory)
                if (item is itemType) { availableEquips.Add(item as itemType); }

            return availableEquips as List<Item>;
        }

        public List<Equipment> GetEquipmentFromInventory(EquipSlot slotType)
        {
            // Get list of EquipType type of items in inventory
            List<Equipment> availableEquips = new List<Equipment>();
            foreach (Item item in Inventory)
                if (item is Equipment)
                {
                    Equipment equip = item as Equipment;
                    if (equip.RequiredSlot == slotType)
                        availableEquips.Add(equip);
                }

            return availableEquips;
        }

        public ItemType ChooseItemFromList<ItemType>(List<ItemType> availableItems) where ItemType : Item
        {
            int choice = Utils.ProcessChoice(availableItems.ConvertAll(x => x.Name).ToArray());
            return availableItems[choice];
        }

        public void ChangeEquipment(Equipment newEquip)
        {
            RemoveFromInventory(newEquip);
            if (equipment.ContainsKey(newEquip.RequiredSlot))
            {
                Equipment oldEquip = equipment[newEquip.RequiredSlot];
                AddToInventory(oldEquip);
                oldEquip.Unequip(this);
            }
            newEquip.Equip(this);

            Utils.Tale("Equipped " + newEquip.Name);
        }
    }

    public abstract class Enemy : Character
    {
        public Enemy(Dictionary<EquipSlot, Equipment> equip = null, List<Spell> spellbook = null) : base()
        {
            if (equip != null)
                foreach (KeyValuePair<EquipSlot, Equipment> e in equip)
                    e.Value.Equip(this);
            else
                equipment[EquipSlot.Weapon] = NaturalWeapon;

            if (spellbook != null)
                this.spellbook = spellbook;
        }

        protected override string LogAction(string actionName)
        {
            return Name + " " + actionName + "s";
        }

        protected override string[] GetBattleActions()
        {
            return new string[] { "Attack" };
        }

        public override void ChooseBattleAction(Character enemy)
        {
            // Now a simple random strategy
            Random rnd = new Random();
            string[] actions = GetBattleActions();
            ExecuteBattleAction(rnd.Next(0, actions.Length), enemy);
        }

        protected override void ChooseSpell(Character target)
        {
            throw new NotImplementedException();
        }
    }
    #endregion


    #region Scenes
    ////////////////////////////////////////////////
    // SCENES
    ////////////////////////////////////////////////
    public class Scene
    {
        public readonly string title;
        private readonly Func<string> func;

        public Scene(string title, Func<string> func)
        {
            this.title = title;
            this.func = func;
        }

        public string Run()
        {
            return func();
        }
    }
    #endregion


    #region Utilities
    ////////////////////////////////////////////////
    // UTILITIES
    ////////////////////////////////////////////////
    public class Die
    {
        public readonly int faces;
        public readonly int quantity;

        public Die(int faces, int quantity = 1)
        {
            this.faces = faces;
            this.quantity = quantity;
        }
    }

    // Static functions
    public static class Utils
    {
        public static int Roll(int faces, int quantity = 1)
        {
            int result = 0;
            for (int i = 0; i < quantity; i++)
            {
                Random rnd = new Random();
                result += rnd.Next(1, faces + 1);
            }
            return result;
        }
        public static int Roll(Die die)
        {
            int result = 0;
            for (int i = 0; i < die.quantity; i++)
            {
                Random rnd = new Random();
                result += rnd.Next(1, die.faces + 1);
            }
            return result;
        }

        public static string WaitInput()
        {
            return Console.ReadLine();
        }

        public static void Tale(string text, bool stop = true)
        {
            foreach (char c in text)
            {
                Console.Write(c);
                Thread.Sleep(3);
            }
            if (stop) { WaitInput(); }
        }

        public static int ProcessChoice(string[] choices)
        {
            int InvalidInput()
            {
                Tale("Invalid input...", stop: false);
                return ProcessChoice(choices);
            }

            string choiceOutput = "";
            for (int i = 0; i < choices.Length; i++)
            {
                choiceOutput += (i + 1).ToString() + ": " + choices[i] + "\n"; // PS: Choices start from 1
            }

            Tale("Choose your action:\n", stop: false);
            Console.WriteLine(choiceOutput);
            string answer = Console.ReadLine();
            int parsedAnswer;
            if (int.TryParse(answer, out parsedAnswer))
            {
                parsedAnswer -= 1; // Choices start from 1
                if (parsedAnswer < 0 || parsedAnswer > choices.Length)
                {
                    return InvalidInput();
                }
                else
                {
                    return parsedAnswer;
                }
            }
            else
            {
                return InvalidInput();
            }
        }
    }

    [System.Serializable]
    public class SaveGame
    {
        public string SceneToLoad { get; set; }
        public List<string> Achievements { get; set; }
        public int PlayerHP { get; set; }
        public List<string> PlayerInventory { get; set; }
        public List<string> PlayerEquipment { get; set; }
        public List<string> Spellbook { get; set; }
    }
    #endregion


    #region Game
    ////////////////////////////////////////////////
    // GAME
    ////////////////////////////////////////////////
    public abstract class Game
    {
        public static string SavePath = "SaveGame.json";
        public static bool gameOver = false;

        protected readonly List<Scene> scenes = new List<Scene>();  // List of all scenes
        protected string sceneToLoad;
        protected List<string> achievements = new List<string>();
        public abstract Player Player { get; }
        public abstract string StartScene { get; }

        public Game()
        {

        }

        public void Save(string fileName)
        {
            string jsonString = JsonSerializer.Serialize(new SaveGame()
            {
                SceneToLoad = sceneToLoad,
                Achievements = achievements,
                PlayerHP = Player.CurrentHP,
                PlayerInventory = Player.GetItemsFromInventory<Item>().ConvertAll<string>(x => x.GetType().ToString()),
                PlayerEquipment = Player.equipment.Values.ToList().ConvertAll<string>(x => x.GetType().ToString()),
                Spellbook = Player.Spellbook.ConvertAll<string>(x => x.GetType().ToString()),
            });
            File.WriteAllText(fileName, jsonString);
        }

        public void Load(string fileName)
        {
            string jsonString = File.ReadAllText(fileName);
            SaveGame saveGame = JsonSerializer.Deserialize<SaveGame>(jsonString);
            sceneToLoad = saveGame.SceneToLoad;
            achievements = saveGame.Achievements;
            Player.CurrentHP = saveGame.PlayerHP;
            foreach (string itemName in saveGame.PlayerInventory)
            {
                Type itemClass = Type.GetType(itemName);
                Item item = Activator.CreateInstance(itemClass) as Item;
                Player.AddToInventory(item);
            }
            foreach (string equipName in saveGame.PlayerEquipment)
            {
                Type equipClass = Type.GetType(equipName);
                Equipment equipment = Activator.CreateInstance(equipClass) as Equipment;
                equipment.Equip(Player);
            }
            foreach (string spellName in saveGame.Spellbook)
            {
                Type spellClass = Type.GetType(spellName);
                Spell spell = Activator.CreateInstance(spellClass) as Spell;
                Player.Spellbook.Add(spell);
            }
        }

        public void AddScene(Scene scene)
        {
            scenes.Add(scene);
        }

        public string PlayScene(string title)
        {
            foreach (Scene scene in scenes)
            {
                if (scene.title == title)
                {
                    Save(SavePath);
                    return scene.Run();
                }
            }

            Console.WriteLine(new Exception("Scene not found"));
            gameOver = true;
            return "";
        }

        public abstract void Initialize();

        public abstract void CreateScenes();

        public virtual void Run()
        {
            CreateScenes();


            //////////////////////////////////////////////
            // LOAD OR START NEW GAME
            //////////////////////////////////////////////
            if (File.Exists(SavePath))
            {
                Utils.Tale("Loading saved game");
                Load(SavePath);
            }
            else
            {
                Utils.Tale("Starting new game");
                sceneToLoad = StartScene;
                Initialize();
            }


            //////////////////////////////////////////////
            // MAIN LOOP
            //////////////////////////////////////////////
            while (!gameOver)
            {
                sceneToLoad = PlayScene(sceneToLoad);
            }

            Utils.Tale("GAME OVER");
            Utils.Tale("PRESS A KEY TO CLOSE THE GAME");
            Console.ReadLine();
        }
    }
    #endregion
}
