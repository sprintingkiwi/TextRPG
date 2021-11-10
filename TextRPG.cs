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

    public abstract class Consumable : Item { }

    public class Potion : Consumable
    {
        public override string Name => "Potion";
        public override int Value => 30;
    }

    public abstract class Equipment : Item
    {
        public abstract Character.EquipSlot RequiredSlot { get; }
        public virtual int Speed { get; }
        public virtual int Durability => -1;
        private int uses = 0;
        public int Uses
        {
            get { return uses; }
            set
            {
                if (Durability < 0) return;
                if (value > Durability)
                    value = Durability;
                uses = value;
            }
        }
        protected Character user;

        public virtual void Equip(Character character)
        {
            user = character;
            character.equipment.Add(RequiredSlot, this);
            character.Speed.SetModifier(Speed, this);
        }

        public virtual void Unequip(Character character)
        {
            user = null;
            character.equipment.Remove(RequiredSlot);
            foreach (Stat stat in character.stats)
                stat.RemoveModifierFromSource(this);
        }

        public virtual void WearOut()
        {
            if (Durability < 0)
                return;
            else
            {
                uses += 1;
                if (uses >= Durability)
                {
                    Game.Instance.Tale(Name + " is broken.");
                    Unequip(user);
                }
            }
        }

        public virtual void Repair()
        {
            if (Durability >= 0)
                uses = 0;
        }
    }

    public abstract class Weapon : Equipment
    {
        public enum DamageType { Slashing, Bludgeoning, Piercing, None }

        public abstract DamageType DmgType { get; } // Type of damage (Slashing, Piercing, ecc.)
        public abstract DamageType Weakness { get; }
        public abstract int Attack { get; }
        public virtual int RndRange { get; set; } // Amount of random in damage
        public virtual int Precision { get; set; } // Chances of hitting the target (%)
        public virtual Action<Character, Character> CustomEffects { get { return (user, target) => { }; } }

        public override Character.EquipSlot RequiredSlot => Character.EquipSlot.Weapon;

        public override void Equip(Character character)
        {
            base.Equip(character);
            character.Attack.SetModifier(Attack, this);
        }
    }

    public class NaturalWeapon : Weapon
    {
        public override DamageType DmgType => DamageType.None;
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
        public override DamageType DmgType => DamageType.Slashing;
        public override DamageType Weakness => DamageType.Piercing;
        public override int RndRange => 10;
        public override int Precision => 90;
        public override int Speed => 0;
    }

    public abstract class Spear : Weapon
    {
        public override DamageType DmgType => DamageType.Piercing;
        public override DamageType Weakness => DamageType.Bludgeoning;
        public override int RndRange => 20;
        public override int Precision => 85;
        public override int Speed => -5;
    }

    public abstract class Hammer : Weapon
    {
        public override DamageType DmgType => DamageType.Bludgeoning;
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
        public override int Durability => 3;
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
        public virtual int ManaCost => 1;
        public virtual Action<Character, Character> CustomEffects { get { return (user, target) => { }; } }
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
        public readonly SortedDictionary<EquipSlot, Equipment> equipment = new SortedDictionary<EquipSlot, Equipment>(); // Dictionary from slot name to item class instance
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
        public Weapon ActiveWeapon
        {
            get
            {
                if (equipment.ContainsKey(EquipSlot.Weapon))
                    return equipment[EquipSlot.Weapon] as Weapon;
                else
                    return NaturalWeapon;
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

        public virtual string ShowStatus()
        {
            return Name + " - HP: " + CurrentHP.ToString();
        }

        protected abstract string LogAction(string actionName);

        public virtual void TakeDamage(int damage, string description = "")
        {
            if (damage < 1) { damage = 1; } // Prevent negative or null damage values
            this.damage += damage;
            Game.Instance.Tale(LogAction("receive") + " " + damage.ToString() + " points of " + description + "damage");
            if (this.damage > MaxHP.Value) { this.damage = MaxHP.Value; } // Prevent damage bigger than MaxHP
        }

        protected void WeaponAttack(Character target)
        {
            Weapon weapon = ActiveWeapon;

            string attackLog = LogAction("attack") + " " + target.Name;
            if (!(weapon is NaturalWeapon)) { attackLog += " with " + weapon.Name; }
            Game.Instance.Tale(attackLog);

            Random rnd = new Random();
            int hitRoll = rnd.Next(1, 100);
            bool hit = hitRoll <= weapon.Precision - target.Speed.Value;
            bool critical = hitRoll > 90;
            if (hit)
            {
                int attackValue = rnd.Next(Attack.Value - weapon.RndRange, Attack.Value + weapon.RndRange + 1);
                int damage = PhysicalDamage(attackValue - target.Defense.Value, weapon.DmgType, target);
                if (critical) { damage *= 3; Game.Instance.Tale("CRITICAL HIT!"); } // Apply critical bonus
                target.TakeDamage(damage);
                weapon.CustomEffects(this, target);
                weapon.WearOut();
            }
            else { Game.Instance.Tale("The attack misses..."); }
        }

        public static int PhysicalDamage(int atkDamage, Weapon.DamageType userType, Character target)
        {
            Weapon targetWeapon = target.ActiveWeapon as Weapon;
            if (targetWeapon.Weakness == userType)
            {
                Game.Instance.Tale("Weapon Advantage!");
                if (target.equipment.ContainsKey(EquipSlot.Shield))
                {
                    Shield targetShield = target.equipment[EquipSlot.Shield] as Shield;
                    Game.Instance.Tale("The shield softens the blow.");
                    targetShield.WearOut();
                }
                else
                {
                    atkDamage = (int)(atkDamage * 1.5f); // Apply weapon weakness
                }
            }
            return atkDamage;
        }

        public virtual void LearnSpell(Spell spell, bool silent = false)
        {
            spellbook.Add(spell);
            if (!silent)
                Game.Instance.Tale("Learned spell: " + spell.Name);
        }

        protected abstract void ChooseSpell(Character target);

        protected virtual void CastSpell(Spell spell, Character target)
        {
            // Cast spell
            Game.Instance.Tale(LogAction("cast") + " the spell: " + spell.Name);
            int damage = MagicDamage(spell.Damage, spell.Element, target.ElementWeakness, target.ElementResistance);
            target.TakeDamage(damage);
            spell.CustomEffects(this, target);
        }

        public static int MagicDamage(int spellDamage, Spell.ElementType damageElement, Spell.ElementType targetWeakness, Spell.ElementType targetResistance)
        {
            if (targetWeakness == damageElement) // Apply element weakness
            {
                spellDamage *= 2;
                Game.Instance.Tale("Element Weakness!");
            }
            else if (targetResistance == damageElement)
            {
                spellDamage /= 2;
                Game.Instance.Tale("Element Resistance!");
            }
            return spellDamage;
        }

        public virtual void DrinkPotion()
        {
            Game.Instance.Tale(LogAction("drink") + " a Potion");
            damage = 0;
            Game.Instance.Tale(Name + " HP: " + CurrentHP);
        }

        public enum BattleOutcome { Continue, Win, Lose }

        public Character Battle(Enemy enemy, Func<BattleOutcome, Enemy, Character> callback = null)
        {
            Game.Instance.Tale("Starting battle with " + enemy.Name);

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
                    //Game.Instance.Tale(enemy.ShowStatus());
                    outcome = CheckOutcome();
                }
                else
                {
                    enemy.WeaponAttack(this);
                    //Game.Instance.Tale(ShowStatus());
                    outcome = CheckOutcome();
                }
                playerTurn = !playerTurn;
            }
            Console.WriteLine("Battle ended");

            if (callback == null)
                return ManageBattleOutcome(outcome, enemy);
            else
                return callback(outcome, enemy);
        }

        public virtual Character ManageBattleOutcome(BattleOutcome outcome, Enemy enemy)
        {
            if (outcome == BattleOutcome.Win)
                return this;
            else
                return enemy;
        }
    }

    public class Player : Character
    {
        protected int manaPoints = 1;
        public override string Name => "You";
        public override Spell.ElementType ElementWeakness => Spell.ElementType.None;
        public override Spell.ElementType ElementResistance => Spell.ElementType.None;
        protected List<Item> Inventory { get; set; } = new List<Item>();
        public Item[] AllItems { get { return equipment.Values.Concat(Inventory).ToArray(); } }
        public Equipment[] AllEquipment { get { return equipment.Values.Concat(Inventory.Where(x => x is Equipment).ToList().ConvertAll(y => y as Equipment)).ToArray(); } }
        public Stat MaxManaPoints { get; } = new Stat();
        public int CurrentManaPoints
        {
            get { return manaPoints; }
            set
            {
                manaPoints = value;
                if (manaPoints > MaxManaPoints.Value)
                    manaPoints = MaxManaPoints.Value;
            }
        }

        public Player()
        {
            MaxHP.SetBaseValue(200);
            Attack.SetBaseValue(10);
            Defense.SetBaseValue(5);
            Speed.SetBaseValue(5);
            MaxManaPoints.SetBaseValue(2);
        }

        protected override string LogAction(string actionName)
        {
            return "You " + actionName;
        }

        public override Character ManageBattleOutcome(BattleOutcome outcome, Enemy enemy)
        {
            if (outcome == BattleOutcome.Win)
                return this;
            else
            {
                Game.Instance.Tale("You died.");
                Game.Instance.Tale("Game Over");
                Game.Instance.WaitInput();
                Environment.Exit(0);
                return enemy;
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
            ExecuteBattleAction(Game.Instance.ProcessChoice(GetBattleActions()), opponent);
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
            Game.Instance.Tale("Bind the spell to its name:\n", stop: false);
            string spellName = Console.ReadLine();

            // Check if spell is in spellbook
            Spell spell = null;
            foreach (Spell s in spellbook)
                if (s.Name == spellName) { spell = s; break; }
            if (spell == null)
            {
                Game.Instance.Tale("You fail to cast the spell...");
                return;
            }

            //Check mana
            if (spell.ManaCost > manaPoints)
            {
                Game.Instance.Tale(spell.Name + " spell casting failed: not enough mana");
                return;
            }
            else
                manaPoints -= spell.ManaCost;

            CastSpell(spell, target);
        }

        public override void DrinkPotion()
        {
            Item[] potions = GetItemsFromInventory("Potion").ToArray();
            if (potions.Length >= 1)
            {
                base.DrinkPotion();
                RemoveFromInventory(GetItemsFromInventory("Potion")[0]);
            }
            else
                Game.Instance.Tale("There are no Potions in your inventory...");
        }

        public void AddToInventory(Item item, bool silent = false)
        {
            if (Inventory.Count <= 10)
            {
                Inventory.Add(item);
                if (!silent)
                    Game.Instance.Tale(item.Name + " added to the inventory.");
            }
            else if (!silent)
                Game.Instance.Tale("You cannot take " + item.Name + ": inventory is full.");
        }

        public void RemoveFromInventory(Item item)
        {
            Inventory.Remove(item);
        }

        public List<Item> GetItemsFromInventory<itemType>() where itemType : Item
        {
            // Get list of EquipType type of items in inventory
            List<itemType> foundItems = new List<itemType>();
            foreach (Item item in Inventory)
                if (item is itemType) { foundItems.Add(item as itemType); }

            return foundItems as List<Item>;
        }
        public List<Item> GetItemsFromInventory(string name)
        {
            // Get list of EquipType type of items in inventory
            List<Item> foundItems = new List<Item>();
            foreach (Item item in Inventory)
                if (item.Name == name) { foundItems.Add(item); }

            return foundItems;
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
            int choice = Game.Instance.ProcessChoice(availableItems.ConvertAll(x => x.Name).ToArray());
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

            Game.Instance.Tale("Equipped " + newEquip.Name);
        }
    }

    public abstract class Enemy : Character
    {
        public Enemy(Dictionary<EquipSlot, Equipment> equip = null, List<Spell> spellbook = null) : base()
        {
            if (equip != null)
                foreach (KeyValuePair<EquipSlot, Equipment> e in equip)
                    e.Value.Equip(this);
            //else
            //    equipment[EquipSlot.Weapon] = NaturalWeapon;

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
    }

    [System.Serializable]
    public class SaveGame
    {
        public string SceneToLoad { get; set; }
        public List<string> Achievements { get; set; }
        public int PlayerHP { get; set; }
        public int PlayerMana { get; set; }
        public List<string> PlayerInventory { get; set; }
        public List<string> PlayerEquipment { get; set; }
        public List<int> DurabilityUsages { get; set; }
        public List<string> Spellbook { get; set; }
    }
    #endregion


    #region Game
    ////////////////////////////////////////////////
    // GAME
    ////////////////////////////////////////////////
    public abstract class Game
    {
        public static Game Instance;

        public const string SavePath = "SaveGame.json";
        protected bool gameOver = false;
        protected readonly SortedList<string, Scene> scenes = new SortedList<string, Scene>();  // List of all scenes
        protected string sceneToLoad;
        protected List<string> achievements = new List<string>();
        protected abstract Player Player { get; }
        protected abstract string StartScene { get; }

        public Game()
        {
            Instance = this;
        }

        public virtual string WaitInput()
        {
            return Console.ReadLine();
        }

        public virtual void Tale(string text, bool stop = true)
        {
            foreach (char c in text)
            {
                Console.Write(c);
                Thread.Sleep(3);
            }
            if (stop) { WaitInput(); }
        }

        public virtual int ProcessChoice(string[] choices)
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

        protected virtual void Save(string fileName = SavePath)
        {
            string jsonString = JsonSerializer.Serialize(new SaveGame()
            {
                SceneToLoad = sceneToLoad,
                Achievements = achievements,
                PlayerHP = Player.CurrentHP,
                PlayerMana = Player.CurrentManaPoints,
                PlayerInventory = Player.GetItemsFromInventory<Item>().ConvertAll(x => x.GetType().ToString()),
                PlayerEquipment = Player.equipment.Values.ToList().ConvertAll(x => x.GetType().ToString()),
                DurabilityUsages = Player.AllEquipment.ToList().ConvertAll(x => x.Uses),
                Spellbook = Player.Spellbook.ConvertAll(x => x.GetType().ToString()),
            });
            File.WriteAllText(fileName, jsonString);
        }

        protected virtual void Load(string fileName)
        {
            string jsonString = File.ReadAllText(fileName);
            SaveGame saveGame = JsonSerializer.Deserialize<SaveGame>(jsonString);
            sceneToLoad = saveGame.SceneToLoad;
            achievements = saveGame.Achievements;
            Player.CurrentHP = saveGame.PlayerHP;
            Player.CurrentManaPoints = saveGame.PlayerMana;
            foreach (string itemName in saveGame.PlayerInventory)
            {
                Type itemClass = Type.GetType(itemName);
                Item item = Activator.CreateInstance(itemClass) as Item;
                Player.AddToInventory(item, silent: true);
            }
            foreach (string equipName in saveGame.PlayerEquipment)
            {
                Type equipClass = Type.GetType(equipName);
                Equipment equipment = Activator.CreateInstance(equipClass) as Equipment;
                equipment.Equip(Player);
            }
            Equipment[] allEquips = Player.AllEquipment;
            for (int i = 0; i < saveGame.DurabilityUsages.Count; i++)
                allEquips[i].Uses = saveGame.DurabilityUsages[i];
            foreach (string spellName in saveGame.Spellbook)
            {
                Type spellClass = Type.GetType(spellName);
                Spell spell = Activator.CreateInstance(spellClass) as Spell;
                Player.LearnSpell(spell, silent: true);
            }
        }

        protected void AddScene(Scene scene)
        {
            if (scenes.ContainsKey(scene.title))
                throw new Exception("Cannot add two scenes with the same title");

            scenes.Add(scene.title, scene);
        }

        protected string PlayScene(string title)
        {
            if (scenes.ContainsKey(title))
            {
                //Save(SavePath);
                return scenes[title].Run();
            }
            else
            {
                Console.WriteLine(new Exception("Scene not found"));
                gameOver = true;
                return "";
            }
        }

        protected abstract void MainMenu();
        protected abstract void Initialize();
        protected abstract void CreateScenes();

        public virtual void Run()
        {
            MainMenu();
            CreateScenes();

            //////////////////////////////////////////////
            // MAIN LOOP
            //////////////////////////////////////////////
            while (!gameOver)
            {
                sceneToLoad = PlayScene(sceneToLoad);
            }

            Tale("GAME OVER");
            Tale("PRESS A KEY TO CLOSE THE GAME");
            Console.ReadLine();
        }
    }
    #endregion
}
