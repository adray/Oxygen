#pragma once
#include <unordered_map>
#include <string>

namespace DE
{
    class ConfigReader;

    enum class EquipmentType
    {
        Helm = 0,
        Chest = 1,
        Legs = 2,
        Gloves = 3,
        Footwear = 4,
        Weapon = 5
    };

    enum class WeaponType
    {
        Pistol = 0,
        DualPistol = 1,
        Sword = 2,
        Whip = 3,
        Rifle = 4,
        Saber = 5
    };

    class ItemConfig_Item
    {
    public:
        ItemConfig_Item(int id, const std::string& name, const std::string& description, int value);
        const int Value() const { return _value; }
        void SetImportant(bool important) { _important = _important; }
        bool IsImportant() const { return _important; }
        bool CanEquip() const { return _canEquip; }
        void SetEquip(bool equip) { _canEquip = equip; }
        const std::string& Name() const { return _name; }
        const std::string& Description() const { return _description; }
        const EquipmentType Type() const { return _type; }
        void SetType(EquipmentType type) { _type = type; }
        bool CanConsume() const { return _canConsume; }
        void SetConsume(bool consume) { _canConsume = consume; }

        void SetAttribute(const std::string& name, const std::string& value);
        int GetIntegerAttribute(const std::string& name) const;

    private:
        void AddInteger(const std::string& name, const std::string& value);
        void AddString(const std::string& name, const std::string& value);
        void AddReal(const std::string& name, const std::string& value);

        int _id;
        std::string _name;
        std::string _description;
        EquipmentType _type;
        bool _canEquip;
        bool _canConsume;
        bool _important;
        int _value;
        WeaponType _weaponType;
        std::unordered_map<std::string, int> _intAttributes;
        std::unordered_map<std::string, float> _realAttributes;
        std::unordered_map<std::string, std::string> _strAttributes;
    };

    class ItemConfig
    {
    public:
        ItemConfig(ConfigReader& cfg);
        void LoadAttributes(ConfigReader& cfg);
        const ItemConfig_Item& Get(int id) { return _items.find(id)->second; }
        const ItemConfig_Item& Get(int id) const { return _items.find(id)->second; }
        const std::vector<std::string> Errors() const { return _errors; }

    private:
        std::unordered_map<int, ItemConfig_Item> _items;
        std::vector<std::string> _errors;
    };
}