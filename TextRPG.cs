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
        private int uses = 0;

        public abstract string Name { get; }
        public abstract int Value { get; }
        public abstract int Weight { get; }
        public abstract int Durability { get; }
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
        public string PrintHealth
        {
            get
            {
                if (Durability > 1)
                    return " [" + (Durability - uses).ToString() + "/" + Durability.ToString() + "]";
                else return "";
            }
        }

        public void Use(Character user)
        {
            OnUse(user);
            if (Durability < 0) return;
            else
            {                
                uses += 1;
                if (uses >= Durability) { OnBreak(); }
            }
        }

        protected abstract void OnUse(Character user);

        protected abstract void OnBreak();

        public virtual void Repair()
        {
            if (Durability >= 0)
                uses = 0;
        }

        [System.Serializable]
        public class SaveData
        {
            public string Type { get; set; }
            public int Uses { get; set; }
        }
        public virtual SaveData GetSaveData()
        {
            return new SaveData()
            {
                Type = GetType().ToString(),
                Uses = uses
            };
        }
    }

    public abstract class Consumable : Item
    {
        public override int Durability => 1;

        protected override void OnBreak() { }
        protected override void OnUse(Character user) { Effect(user); }
        protected abstract void Effect(Character user);
    }

    public class Potion : Consumable
    {
        public override string Name => "Potion";
        public override int Value => 30;
        public override int Weight => 0;
        protected override void Effect(Character user)
        {
            user.CurrentHP = user.MaxHP.Value;
            Game.Instance.Tale(user.Name + " HP: " + user.CurrentHP);
        }
    }

    public abstract class Equipment : Item
    {
        protected Character user;
        public override int Durability => -1;
        public abstract Character.EquipSlot RequiredSlot { get; }

        protected override void OnUse(Character user) { }

        protected override void OnBreak()
        {
            Game.Instance.Tale(Name + " is broken.");
            user.UnEquip(this);
        }

        public virtual void OnEquip(Character character)
        {
            user = character;
            character.Speed.SetModifier(-Weight, this);
        }

        public virtual void OnUnequip(Character character)
        {
            user = null;
            foreach (Stat stat in character.stats)
                stat.RemoveModifierFromSource(this);
        }        
    }

    public abstract class Weapon : Equipment
    {
        public enum DamageType { Slashing, Bludgeoning, Piercing, Natural, None }
        public abstract DamageType DmgType { get; } // Type of damage (Slashing, Piercing, ecc.)
        public abstract DamageType Weakness { get; }
        public abstract int Attack { get; }
        public abstract int RndRange { get; } // Amount of random in damage
        public abstract int Precision { get; } // Chances of hitting the target (%)
        public abstract int Critical { get; } // Chances of critical hit (%)
        public virtual Action<Character, Character> CustomEffects { get { return (user, target) => { }; } }
        public override Character.EquipSlot RequiredSlot => Character.EquipSlot.Weapon;

        public override void OnEquip(Character character)
        {
            base.OnEquip(character);
            character.Attack.SetModifier(Attack, this);
        }

        public virtual void OnHit(Character user, Character target)
        {
            CustomEffects(user, target);
        }
    }

    public class NaturalWeapon : Weapon
    {
        public override DamageType DmgType => DamageType.Natural;
        public override DamageType Weakness => DamageType.None;
        public override string Name => "";
        public override int Value => 0;
        public override int Attack => 0;
        public override int RndRange { get; }
        public override int Precision { get; }
        public override int Critical { get; }
        public override int Weight => 0;

        public NaturalWeapon(int rndRange, int precision, int critical = 10)
        {
            RndRange = rndRange;
            Precision = precision;
            Critical = critical;
        }
    }

    public abstract class Sword : Weapon
    {
        public override DamageType DmgType => DamageType.Slashing;
        public override DamageType Weakness => DamageType.Piercing;
        public override int RndRange => 5;
        public override int Precision => 90;
        public override int Critical => 10;
        public override int Weight => 3;
    }

    public abstract class Spear : Weapon
    {
        public override DamageType DmgType => DamageType.Piercing;
        public override DamageType Weakness => DamageType.Bludgeoning;
        public override int RndRange => 10;
        public override int Precision => 85;
        public override int Critical => 10;
        public override int Weight => 3;
    }

    public abstract class Hammer : Weapon
    {
        public override DamageType DmgType => DamageType.Bludgeoning;
        public override DamageType Weakness => DamageType.Slashing;
        public override int RndRange => 15;
        public override int Precision => 80;
        public override int Critical => 20;
        public override int Weight => 5;
    }

    public abstract class Armor : Equipment
    {
        public abstract int Defense { get; }

        public override void OnEquip(Character character)
        {
            base.OnEquip(character);
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
        public override int Weight => 2;
        public override int Defense => 0;
    }

    public abstract class Accessory : Equipment
    {
        public override Character.EquipSlot RequiredSlot => Character.EquipSlot.Accessory;
        public override int Weight => 0;
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

        public class SaveData
        {
            public string Type { get; set; }
        }
        public virtual SaveData GetSaveData()
        {
            return new SaveData()
            {
                Type = GetType().ToString()
            };
        }
    }
    #endregion


    #region Characters
    ////////////////////////////////////////////////
    // CHARACTERS
    ////////////////////////////////////////////////
    public class Stat
    {
        private int baseValue;
        private Dictionary<Equipment, int> modifiers = new Dictionary<Equipment, int>();
        public string Name { get; }

        public Stat(string name)
        {
            Name = name;
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

        public void SetBaseValue(int baseValue)
        {
            this.baseValue = baseValue;
        }
        public void ChangeBaseValue(int deltaValue)
        {
            baseValue += deltaValue;
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

        [System.Serializable]
        public class SaveData
        {
            public string Name { get; set; }
            public int BaseValue { get; set; }            
        }
        public virtual SaveData GetSaveData()
        {
            return new SaveData()
            {
                BaseValue = baseValue,
                Name = Name
            };
        }
    }

    public abstract class Character
    {
        protected int damage; // Amount of damage taken so far
        public readonly Stat[] stats; // Collection of character's stats
        protected SortedDictionary<EquipSlot, Equipment> equipment = new SortedDictionary<EquipSlot, Equipment>(); // Dictionary from slot name to item class instance
        protected List<Spell> spellbook = new List<Spell>(); // Known spells
        //public int level = 1;

        public enum EquipSlot { Head, Body, Shield, Weapon, Accessory }
        public abstract string Name { get; } // Total hit points
        public Stat MaxHP { get; } = new Stat("Hit Points"); // Total hit points
        public Stat Attack { get; } = new Stat("Attack");
        public Stat Defense { get; } = new Stat("Defense");
        public Stat Speed { get; } = new Stat("Speed");
        public abstract Spell.ElementType ElementWeakness { get; }
        public abstract Spell.ElementType ElementResistance { get; }
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
        public Equipment[] EquippedItems { get { return equipment.Values.ToArray(); } }
        public Character() // Characters initialization
        {
            stats = new Stat[] { MaxHP, Attack, Defense, Speed };
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

        public virtual void ShowStatus()
        {
            Game.Instance.Tale(Name + " - HP: " + CurrentHP.ToString());
        }

        protected abstract string LogAction(string actionName);
        public Equipment GetEquipment(EquipSlot slot)
        {
            return equipment[slot];
        }

        public virtual void Equip(Equipment newEquip, bool silent = false)
        {
            if (equipment.ContainsKey(newEquip.RequiredSlot))
                equipment[newEquip.RequiredSlot] = newEquip;
            else
                equipment.Add(newEquip.RequiredSlot, newEquip);

            newEquip.OnEquip(this);

            if (!silent)
                Game.Instance.Tale("Equipped " + newEquip.Name);
        }

        public virtual void UnEquip(Equipment newEquip, bool silent = false)
        {
            equipment[newEquip.RequiredSlot].OnUnequip(this);

            if (equipment.ContainsKey(newEquip.RequiredSlot))
                equipment.Remove(newEquip.RequiredSlot);

            if (!silent)
                Game.Instance.Tale("Unequipped " + newEquip.Name);
        }

        public virtual void TakeDamage(int damage, string description = "")
        {
            if (damage < 1) { damage = 1; } // Prevent negative or null damage values
            this.damage += damage;
            Game.Instance.Tale(LogAction("receive") + " " + damage.ToString() + " points of " + description + "damage");
            if (this.damage > MaxHP.Value) { this.damage = MaxHP.Value; } // Prevent damage bigger than MaxHP
        }

        public virtual void OnGetHit(int damage)
        {
            TakeDamage(damage);
        }

        protected void WeaponAttack(Character target)
        {
            Weapon weapon = ActiveWeapon;
            string attackLog = LogAction("attack") + " " + target.Name;
            if (!(weapon is NaturalWeapon)) { attackLog += " with " + weapon.Name; }
            Game.Instance.Tale(attackLog);

            // Random rolls
            Random rnd = new Random();
            int hitRoll = rnd.Next(1, 100);
            bool hit = hitRoll >= 100 - weapon.Precision + target.Speed.Value;
            bool critical = hitRoll >= (100 - weapon.Critical);
            
            if (hit)
            {
                // Damage calculations
                int attackValue = rnd.Next(Attack.Value - weapon.RndRange, Attack.Value + weapon.RndRange + 1);
                if (!critical) { attackValue -= target.Defense.Value; }
                int damage = PhysicalDamage(attackValue, weapon.DmgType, target.ActiveWeapon.Weakness);
                if (critical) { damage *= 3; Game.Instance.Tale("CRITICAL HIT!"); } // Apply critical bonus
                
                // Events call
                target.OnGetHit(damage);
                weapon.OnHit(this, target);
                weapon.Use(this);
            }
            else { Game.Instance.Tale("The attack misses..."); }
        }

        public static int PhysicalDamage(int atkDamage, Weapon.DamageType userType, Weapon.DamageType targetWeakness)
        {
            bool weaponAdvantage = targetWeakness == userType;
            //bool useShield = false;            
            if (weaponAdvantage)
            {
                Game.Instance.Tale("Weapon Advantage!"); // Alert for weapon advantage
                atkDamage = (int)(atkDamage * 1.5f); // Apply weapon weakness

                // Ask to use the shield if target has a shield
                //if (target.equipment.ContainsKey(EquipSlot.Shield))
                //    useShield = target.UseShield();
                //if (useShield)
                //    target.equipment[EquipSlot.Shield].WearOut();
                //else
                //    atkDamage = (int)(atkDamage * 1.5f); // Apply weapon weakness
            }
            return atkDamage;
        }

        //public virtual bool UseShield() { return false; }

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

        protected abstract void DrinkPotion();

        public virtual void UseConsumable(Consumable consumable)
        {
            Game.Instance.Tale(LogAction("drink") + " " + consumable.Name);
            consumable.Use(this);
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
            Game.Instance.Tale("Battle ended");

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

        [System.Serializable]
        public class SaveData
        {
            public List<Stat.SaveData> Stats { get; set; }
            public int CurrentHP { get; set; }
            public List<Item.SaveData> Equipment { get; set; }
            public List<Spell.SaveData> Spellbook { get; set; }
        }
        public virtual SaveData GetSaveData()
        {
            return new SaveData()
            {
                Stats = stats.ToList().ConvertAll(x => x.GetSaveData()),
                CurrentHP = CurrentHP,
                Equipment = EquippedItems.ToList().ConvertAll(x => x.GetSaveData()),
                Spellbook = Spellbook.ConvertAll(x => x.GetSaveData())
            };
        }
    }

    public class Player : Character
    {
        protected int manaPoints = 1;
        protected int inventoryCapability = 7;
        protected readonly List<Item> inventory = new List<Item>();

        public override string Name => "You";
        public override Spell.ElementType ElementWeakness => Spell.ElementType.None;
        public override Spell.ElementType ElementResistance => Spell.ElementType.None;
        public Stat MaxManaPoints { get; } = new Stat("Mana");
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
        public Item[] Inventory {get {return inventory.ToArray(); } }
        public List<string> InventoryNames { get { return inventory.ConvertAll(x => x.Name); } }
        public Item[] AllItems { get { return equipment.Values.Concat(inventory).ToArray(); } }
        public List<string> AllItemsNames { get { return AllItems.ToList().ConvertAll(x => x.Name); } }        
        public Equipment[] AllEquipment { get { return equipment.Values.Concat(inventory.Where(x => x is Equipment).ToList().ConvertAll(y => y as Equipment)).ToArray(); } }
        public List<string> AllEquipmentNames { get { return AllEquipment.ToList().ConvertAll(x => x.Name); } }
        public EquipSlot[] EquippableSlots
        {
            get
            {
                List<EquipSlot> slots = new List<EquipSlot>();
                Equipment[] storedEquips = GetItemsFromInventory<Equipment>().ToArray();
                foreach (Equipment equip in storedEquips)
                    if (!slots.Contains(equip.RequiredSlot))
                        slots.Add(equip.RequiredSlot);
                return slots.ToArray();
            }
        }
        public List<string> EquippableSlotsNames { get { return EquippableSlots.ToList().ConvertAll(x => x.ToString()); } }

        public Player()
        {
            MaxHP.SetBaseValue(200);
            Attack.SetBaseValue(10);
            Defense.SetBaseValue(10);
            Speed.SetBaseValue(10);
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
            ExecuteBattleAction(Game.Instance.ProcessChoice(GetBattleActions(), "Choose your action:"), opponent);
        }

        //public override bool UseShield()
        //{
        //    switch (Game.Instance.ProcessChoice(new string[] { "YES", "NO" }, "Use the Shield?"))
        //    {
        //        case 0:
        //            Game.Instance.Tale("The shield softens the blow.");
        //            return true;
        //        default: return false;
        //    }
        //}

        protected override void ExecuteBattleAction(int actionID, Character opponent)
        {
            base.ExecuteBattleAction(actionID, opponent);

            switch (GetBattleActions()[actionID])
            {
                case "Change Weapon":
                    if (GetEquipmentFromInventory(EquipSlot.Weapon).Count > 0)
                        EquipFromInventory(ChooseItemFromList(GetEquipmentFromInventory(EquipSlot.Weapon)));
                    else
                        Game.Instance.Tale("You don't have any weapon in your inventory.");
                    break;
                default:
                    break;
            }
        }

        protected override void ChooseSpell(Character target)
        {
            Game.Instance.Tale("Bind the spell to its name:\n", stop: false);
            string spellName = Game.Instance.WaitInput();

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

        protected override void DrinkPotion()
        {
            List<Potion> potions = GetItemsFromInventory<Potion>();
            if (potions.Count >= 1)
            {
                string[] choices = potions.ConvertAll(x => x.Name).ToArray();
                int choice = Game.Instance.ProcessChoice(choices);
                Consumable chosenConsumable = potions[choice];
                base.UseConsumable(chosenConsumable);
                RemoveFromInventory(chosenConsumable);
            }
            else
                Game.Instance.Tale("There are no Potions in your inventory...");
        }

        public void AddToInventory(Item item, bool silent = false)
        {
            if (inventory.Count < inventoryCapability)
            {
                inventory.Add(item);
                if (!silent)
                    Game.Instance.Tale(item.Name + " added to the inventory.");
            }
            else
            {
                Game.Instance.Tale("You cannot take " + item.Name + ": inventory is full.");
                switch (Game.Instance.ProcessChoice
                    (new string[] { "YES", "NO" },
                    "Drop an item to free space?"))
                {
                    case 0: AskDropItem(); break;
                    case 1: break;
                }
            }
        }

        public bool RemoveFromInventory(Item item)
        {
            if (inventory.Contains(item))
            {
                inventory.Remove(item);
                //Game.Instance.Tale(item.Name + " removed from the inventory.");
                return true;
            }
            else
                return false;
        }

        protected void AskDropItem()
        {
            List<string> choices = InventoryNames;
            choices.Add("Back");
            int choice = Game.Instance.ProcessChoice(choices.ToArray(),
                "Which item do you want to drop?");
            if (choice != choices.Count - 1)
            {
                Item droppingItem = inventory[choice];
                RemoveFromInventory(droppingItem);
                Game.Instance.Tale("Dropped " + droppingItem.Name);
            }
        }

        public List<itemType> GetItemsFromInventory<itemType>() where itemType : Item
        {
            List<itemType> foundItems = inventory.Where(x => x is itemType).ToList().ConvertAll(x => x as itemType);
            return foundItems;
        }
        public List<Item> GetItemsFromInventory(string name)
        {
            // Get list of EquipType type of items in inventory
            List<Item> foundItems = new List<Item>();
            foreach (Item item in inventory)
                if (item.Name == name) { foundItems.Add(item); }

            return foundItems;
        }

        public List<Equipment> GetEquipmentFromInventory(EquipSlot slotType)
        {
            // Get list of EquipType type of items in inventory
            List<Equipment> availableEquips = new List<Equipment>();
            foreach (Item item in inventory)
                if (item is Equipment)
                {
                    Equipment equip = item as Equipment;
                    if (equip.RequiredSlot == slotType)
                        availableEquips.Add(equip);
                }

            return availableEquips;
        }

        public Equipment GetEquipment(string name)
        {
            foreach (Equipment equip in AllEquipment)
                if (equip.Name == name)
                    return equip;
            return null;
        }

        public ItemType ChooseItemFromList<ItemType>(List<ItemType> availableItems) where ItemType : Item
        {
            int choice = Game.Instance.ProcessChoice(availableItems.ConvertAll(x => x.Name).ToArray());
            return availableItems[choice];
        }

        protected void EquipFromInventory(Equipment newEquip)
        {
            if (!inventory.Contains(newEquip))
            {
                Game.Instance.Tale("Cannot equip " + newEquip.Name + ": not in Inventory");
                return;
            }

            RemoveFromInventory(newEquip);
            if (equipment.ContainsKey(newEquip.RequiredSlot))
            {
                Equipment oldEquip = equipment[newEquip.RequiredSlot];
                AddToInventory(oldEquip);
                UnEquip(oldEquip, silent: true);
            }
            Equip(newEquip);
        }

        // Find equipment and ask what to do
        public virtual void FindEquipment(Equipment newEquip)
        {
            foreach (Equipment equip in AllEquipment)
                if (equip.Name == newEquip.Name)
                {
                    equip.Repair();
                    Game.Instance.Tale("Replaced " + newEquip.Name + " with a new one.");
                    
                    if (equipment.ContainsValue(equip))
                        return; // In this case, there's no need to ask to equip it
                }
            if (equipment.ContainsKey(newEquip.RequiredSlot))
            {
                switch (Game.Instance.ProcessChoice
                    (new string[] { "YES", "NO" },
                    "Replace " + equipment[newEquip.RequiredSlot] + " with " + newEquip.Name + "?"))
                {
                    case 0:
                        AddToInventory(equipment[newEquip.RequiredSlot]);
                        Equip(newEquip);
                        break;
                    case 1: break;
                }
            }
            else
            {
                switch (Game.Instance.ProcessChoice
                    (new string[] { "YES", "NO" },
                    "Equip the " + newEquip.Name + "?"))
                {
                    case 0: Equip(newEquip); break;
                    case 1: AddToInventory(newEquip); break;
                }
            }
        }

        // Process choices to change equipped items
        protected void ChangeEquipment()
        {
            if (EquippableSlots.Length < 1)
            {
                Game.Instance.Tale("You have no equippable items in your inventory.");
                return;
            }

            int chosenSlot = Game.Instance.ProcessChoice(EquippableSlotsNames.ToArray());
            List<Equipment> equipsToChoose = GetEquipmentFromInventory(EquippableSlots[chosenSlot]);
            int chosenEquip = Game.Instance.ProcessChoice(equipsToChoose.ConvertAll(x => x.Name).ToArray());
            EquipFromInventory(equipsToChoose[chosenEquip]);
        }

        [System.Serializable]
        public class PlayerSaveData : SaveData
        {
            public int CurrentMana { get; set; }
            public List<Item.SaveData> Inventory { get; set; }
        }
        public override SaveData GetSaveData()
        {
            SaveData saveData = base.GetSaveData();
            return new PlayerSaveData
            {
                // Get generic character informations
                Stats = saveData.Stats,
                CurrentHP = saveData.CurrentHP,
                Equipment = saveData.Equipment,
                Spellbook = saveData.Spellbook,

                // Adding player-specific informations
                CurrentMana = CurrentManaPoints,
                Inventory = Inventory.ToList().ConvertAll(x => x.GetSaveData())
            };
        }
    }

    public abstract class Enemy : Character
    {
        public Enemy(Dictionary<EquipSlot, Equipment> equip = null, List<Spell> spellbook = null) : base()
        {
            if (equip != null)
                foreach (KeyValuePair<EquipSlot, Equipment> e in equip)
                    Equip(e.Value, silent: true);
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

        //public override bool UseShield()
        //{
        //    Random rnd = new Random();
        //    if (rnd.Next(0, 99) < 80)
        //    {
        //        Game.Instance.Tale(Name + " protects himself with a shield.");
        //        return true;
        //    }                
        //    else return false;
        //}

        protected override void ChooseSpell(Character target)
        {
            throw new NotImplementedException();
        }

        protected override void DrinkPotion()
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

    public class Music
    {
        public virtual Note[] Tune => new Note[]
        {
            new Note(Tone.B, Duration.QUARTER),
            new Note(Tone.A, Duration.QUARTER),
            new Note(Tone.GbelowC, Duration.QUARTER),
            new Note(Tone.A, Duration.QUARTER),
            new Note(Tone.B, Duration.QUARTER),
            new Note(Tone.B, Duration.QUARTER),
            new Note(Tone.B, Duration.HALF),
            new Note(Tone.A, Duration.QUARTER),
            new Note(Tone.A, Duration.QUARTER),
            new Note(Tone.A, Duration.HALF),
            new Note(Tone.B, Duration.QUARTER),
            new Note(Tone.D, Duration.QUARTER),
            new Note(Tone.D, Duration.HALF)
        };

        //Thread musicThread;

        public Music()
        {
            Play();
            //musicThread = new Thread(Play);
            //musicThread.Start();
        }

        // Play the notes in a song.
        public void Play()
        {
            foreach (Note n in Tune)
            {
                if (n.NoteTone == Tone.REST)
                    Thread.Sleep((int)n.NoteDuration);
                else
                    Console.Beep((int)n.NoteTone, (int)n.NoteDuration);
            }
        }

        // Define the frequencies of notes in an octave, as well as
        // silence (rest).
        public enum Tone
        {
            REST = 0,
            GbelowC = 196,
            A = 220,
            Asharp = 233,
            B = 247,
            C = 262,
            Csharp = 277,
            D = 294,
            Dsharp = 311,
            E = 330,
            F = 349,
            Fsharp = 370,
            G = 392,
            Gsharp = 415,
        }

        // Define the duration of a note in units of milliseconds.
        public enum Duration
        {
            WHOLE = 1600,
            HALF = WHOLE / 2,
            QUARTER = HALF / 2,
            EIGHTH = QUARTER / 2,
            SIXTEENTH = EIGHTH / 2,
        }

        // Define a note as a frequency (tone) and the amount of
        // time (duration) the note plays.
        public struct Note
        {
            Tone toneVal;
            Duration durVal;

            // Define a constructor to create a specific note.
            public Note(Tone frequency, Duration time)
            {
                toneVal = frequency;
                durVal = time;
            }

            // Define properties to return the note's tone and duration.
            public Tone NoteTone { get { return toneVal; } }
            public Duration NoteDuration { get { return durVal; } }
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
    #endregion


    #region Game
    ////////////////////////////////////////////////
    // GAME
    ////////////////////////////////////////////////
    public abstract class Game
    {
        public static Game Instance;

        protected bool gameOver = false;
        protected readonly SortedList<string, Scene> scenes = new SortedList<string, Scene>();  // List of all scenes
        protected string sceneToLoad;
        protected List<string> achievements = new List<string>();

        public virtual string SavePath => "SaveGame.json";
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

        public virtual void Tale(string text, bool stop = true, bool progressive = true)
        {
            if (progressive)
                foreach (char c in text)
                {
                    Console.Write(c);
                    Thread.Sleep(3);
                }
            else
                Console.Write(text);

            if (stop) { WaitInput(); }
            else { Console.Write("\n"); }
        }

        public virtual int ProcessChoice(string[] choices, string prompt = "")
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

            if (prompt != "") { prompt = "\n" + prompt; }
            Console.Write(prompt + "\n");
            Console.WriteLine(choiceOutput);
            string answer = WaitInput();
            int parsedAnswer;
            if (int.TryParse(answer, out parsedAnswer))
            {
                parsedAnswer -= 1; // Choices start from 1
                if (parsedAnswer < 0 || parsedAnswer >= choices.Length)
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

        [System.Serializable]
        public class SaveGame
        {
            public string SceneToLoad { get; set; }
            public List<string> Achievements { get; set; }
            public Player.PlayerSaveData PlayerData { get; set; }
        }

        protected virtual void Save(string fileName)
        {
            string jsonString = JsonSerializer.Serialize(new SaveGame()
            {
                SceneToLoad = sceneToLoad,
                Achievements = achievements,
                PlayerData = Player.GetSaveData() as Player.PlayerSaveData
            });
            File.WriteAllText(fileName, jsonString);
        }

        protected virtual void Load(string fileName)
        {
            string jsonString = File.ReadAllText(fileName);
            SaveGame saveGame = JsonSerializer.Deserialize<SaveGame>(jsonString);
            sceneToLoad = saveGame.SceneToLoad;
            achievements = saveGame.Achievements;
            Player.CurrentHP = saveGame.PlayerData.CurrentHP;
            Player.CurrentManaPoints = saveGame.PlayerData.CurrentMana;
            for (int i=0; i < Player.stats.Count(); i++)
            {
                Player.stats[i].SetBaseValue(saveGame.PlayerData.Stats[i].BaseValue);
            }
            foreach (Item.SaveData itemData in saveGame.PlayerData.Inventory)
            {
                Item item = Activator.CreateInstance(Type.GetType(itemData.Type)) as Item;
                item.Uses = itemData.Uses;
                Player.AddToInventory(item, silent: true);
            }
            foreach (Item.SaveData equipData in saveGame.PlayerData.Equipment)
            {
                Equipment equip = Activator.CreateInstance(Type.GetType(equipData.Type)) as Equipment;
                equip.Uses = equipData.Uses;
                Player.Equip(equip, silent: true);
            }
            foreach (Spell.SaveData spellData in saveGame.PlayerData.Spellbook)
            {
                Spell spell = Activator.CreateInstance(Type.GetType(spellData.Type)) as Spell;
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
            WaitInput();
        }
    }
    #endregion
}
