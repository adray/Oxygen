#pragma once
#include <string>
#include <vector>
#include <unordered_map>
#include <memory>

namespace DE
{
    class Party_Pack;
    class ItemConfig;

    class Party_Stat_Curve
    {
    public:
        Party_Stat_Curve() : _curveType(0), _min(0), _max(0) {}
        Party_Stat_Curve(int curveType, int min, int max) : _curveType(curveType), _min(min), _max(max) {}

        int Calculate(int level) const;

    private:
        int Curve0(int level) const;
        int Curve1(int level) const;
        int Curve2(int level) const;

        int _curveType;
        int _min; int _max;
    };

    class Party_Member_Stats
    {
    public:
        Party_Member_Stats() : _maxDP(0), _maxHP(0), _attack(0), _defence(0) {}

        inline int MaxHP() const { return _maxHP; }
        inline int MaxDP() const { return _maxDP; }
        inline int Attack() const { return _attack; }
        inline int Defence() const { return _defence; }

        void Reset();

        inline void SetMaxHP(int hp) { _maxHP = hp; }
        inline void SetMaxDP(int dp) { _maxDP = dp; }
        inline void SetAttack(int attack) { _attack = attack; }
        inline void SetDefence(int defence) { _defence = defence; }

        inline void SetAttackCurve(Party_Stat_Curve curve) { _attackCurve = curve; }
        inline void SetDefenceCurve(Party_Stat_Curve curve) { _defenceCurve = curve; }
        inline void SetMaxHPCurve(Party_Stat_Curve curve) { _maxHPCurve = curve; }

        inline const Party_Stat_Curve& AttackCurve() const { return _attackCurve; }
        inline const Party_Stat_Curve& DefenceCurve() const { return _defenceCurve; }
        inline const Party_Stat_Curve& MaxHPCurve() const { return _maxHPCurve; }

    private:
        int _maxHP;
        int _maxDP;
        int _attack;
        int _defence;

        Party_Stat_Curve _attackCurve;
        Party_Stat_Curve _maxHPCurve;
        Party_Stat_Curve _maxDPCurve;
        Party_Stat_Curve _defenceCurve;
    };

    class Party_Member
    {
    public:
        Party_Member(const std::string& name, int id, int level);
        inline const std::string& Name() const { return _name; }
        inline int ID() const { return _id; }
        inline int Level() const { return _level; }
        inline bool Active() const { return _active; }
        inline void SetActive(bool active) { _active = active; }
        inline int XP() const { return _exp; }
        inline void SetXP(int exp) { _exp = exp; }
        inline void SetLevel(int level) { _level = level; }
        inline int HP() const { return _health; };
        inline void SetHP(int hp) { _health = hp; }

        inline int Helm() const { return _helm; }
        inline int Footwear() const { return _footwear; }
        inline int Chest() const { return _chest; }
        inline int Legs() const { return _legs; }
        inline int Gloves() const { return _gloves; }
        inline int PrimaryWeapon() const { return _weaponPrimary; }
        inline int SecondaryWeapon() const { return _weaponSecondary; }

        inline void SetHelm(int id) { _helm = id; }
        inline void SetFootwear(int id) { _footwear = id; }
        inline void SetChest(int id) { _chest = id; }
        inline void SetLegs(int id) { _legs = id; }
        inline void SetGloves(int id) { _gloves = id; }
        inline void SetPrimaryWeapon(int id) { _weaponPrimary = id; }
        inline void SetSecondaryWeapon(int id) { _weaponSecondary = id; }

        void CalculateStats(const Party_Pack& pack, const std::shared_ptr<ItemConfig>& cfg);
        inline const Party_Member_Stats& GetStats() const { return _stats; }
        inline Party_Member_Stats& GetStats() { return _stats; }

    private:
        Party_Member_Stats _stats;
        std::string _name;
        bool _active;
        int _id;
        int _level;
        int _exp;
        int _helm;
        int _footwear;
        int _chest;
        int _legs;
        int _gloves;
        int _weaponPrimary;
        int _weaponSecondary;
        int _health;
        int _dp;
    };

    class Party_Item
    {
    public:
        Party_Item(int id, int quantity)
            : _id(id), _quantity(quantity), _numEquipped(0){}
        Party_Item() : Party_Item(-1, 0) {};
        inline void Increment() { _quantity++; };
        inline void Decrement() { _quantity--; }
        inline void Increment(int count) { _quantity += count; };
        inline void Decrement(int count) { _quantity -= count; };
        inline int NumItems() const {
            return _quantity;
        }
        inline int ID() const {
            return _id;
        }
        inline bool SetEquipped(int num) {
            if (_numEquipped + num <= _quantity)
            {
                _numEquipped += num;
                return true;
            }
            return false;
        };
        inline bool Unequip(int num) {
            if (_numEquipped - num >= 0)
            {
                _numEquipped -= num;
                return true;
            }
            return false;
        }
        inline int NumEquipped() const { return _numEquipped; }

    private:
        int _id;
        int _quantity;
        int _numEquipped;
    };

    class Party_Pack
    {
    public:
        void AddItem(int id, int quantity);
        void RemoveItem(int id, int quantity);
        bool Equip(int id);
        bool Unequip(int id);
        inline Party_Item& For(int id) { return _items[id]; }
        inline const Party_Item& For(int id) const { return _items.find(id)->second; }
        inline Party_Item& At(int index) { return _items[_ids[index]]; }
        inline const Party_Item& At(int index) const { return _items.find(_ids[index])->second; }
        int NumItems() const { return int(_ids.size()); }

    private:
        std::unordered_map<int, Party_Item> _items;
        std::vector<int> _ids;
    };

    class Party
    {
    public:
        Party();
        Party_Member& FindMemberByID(int id);
        const Party_Member& FindMemberByID(int id) const;
        inline Party_Member& FindMemberByIndex(int index) {
            return _members[index];
        }
        inline const Party_Member& FindMemberByIndex(int index) const {
            return _members[index];
        }
        inline Party_Pack& Pack() { return _pack; }
        inline const Party_Pack& Pack() const { return _pack; }
        inline int NumMembers() const {
            return int(_members.size());
        }

    private:
        int AddMember(const Party_Member& member);

        Party_Member _unknown;
        std::vector<Party_Member> _members;
        Party_Pack _pack;
    };
}
