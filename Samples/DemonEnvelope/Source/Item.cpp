#include "Item.h"
#include "ConfigReader.h"

using namespace DE;

ItemConfig_Item::ItemConfig_Item(int id, const std::string& name, const std::string& description, int value)
    :
    _id(id),
    _name(name),
    _description(description),
    _value(value),
    _canEquip(false),
    _canConsume(false),
    _important(false),
    _type(EquipmentType::Helm),
    _weaponType(WeaponType::Pistol)
{
}

void ItemConfig_Item::SetAttribute(const std::string& name, const std::string& value)
{
    if (name == "Damage" ||
        name == "Defence" ||
        name == "NumTargets" ||
        name == "Heal" ||
        name == "Accuracy")
    {
        AddInteger(name, value);
    }
    else if (name == "Type")
    {
        if (value == "Weapon")
        {
            _type = EquipmentType::Weapon;
            _canEquip = true;
        }
        else if (value == "Helm")
        {
            _type = EquipmentType::Helm;
            _canEquip = true;
        }
        else if (value == "Chest")
        {
            _type = EquipmentType::Chest;
            _canEquip = true;
        }
        else if (value == "Legs")
        {
            _type = EquipmentType::Legs;
            _canEquip = true;
        }
        else if (value == "Gloves")
        {
            _type = EquipmentType::Gloves;
            _canEquip = true;
        }
        else if (value == "Footwear")
        {
            _type = EquipmentType::Footwear;
            _canEquip = true;
        }
    }
    else if (name == "Target")
    {
        if (value == "Friendly")
        {
            _intAttributes.insert(std::pair<std::string, int>(name, 1));
        }
        else if (value == "Enemy")
        {
            _intAttributes.insert(std::pair<std::string, int>(name, 2));
        }
        else if (value == "All")
        {
            _intAttributes.insert(std::pair<std::string, int>(name, 3));
        }
    }
    else if (name == "Consumable")
    {
        _canConsume = value == "True";
    }
    else if (name == "Important")
    {
        _important = value == "True";
    }
}

void ItemConfig_Item::AddInteger(const std::string& name, const std::string& value)
{
    _intAttributes.insert(std::pair<std::string, int>(name, std::strtol(value.c_str(), 0, 10)));
}

void ItemConfig_Item::AddString(const std::string& name, const std::string& value)
{
    _strAttributes.insert(std::pair<std::string, std::string>(name, value));
}

void ItemConfig_Item::AddReal(const std::string& name, const std::string& value)
{
    _realAttributes.insert(std::pair<std::string, float>(name, std::strtof(value.c_str(), 0)));
}

int ItemConfig_Item::GetIntegerAttribute(const std::string& name) const
{
    const auto& it = _intAttributes.find(name);
    if (it != _intAttributes.end())
    {
        return it->second;
    }

    return 0;
}

ItemConfig::ItemConfig(ConfigReader& cfg)
{
    while (cfg.ReadNextRow())
    {
        const int id = std::strtol(cfg.Get("id").c_str(), 0, 10);
        const std::string name = cfg.Get("name");
        const std::string description = cfg.Get("description");
        const int value = std::strtol(cfg.Get("value").c_str(), 0, 10);

        ItemConfig_Item item(id, name, description, value);
        _items.insert(std::pair<int, ItemConfig_Item>(id, item));
    }
}

void ItemConfig::LoadAttributes(ConfigReader& cfg)
{
    while (cfg.ReadNextRow())
    {
        const int id = std::strtol(cfg.Get("id").c_str(), 0, 10);
        const std::string name = cfg.Get("name");
        const std::string value = cfg.Get("value");

        const auto& it = _items.find(id);
        if (it != _items.end())
        {
            it->second.SetAttribute(name, value);
        }
        else
        {
            _errors.push_back("Unable to add attribute " + name);
        }
    }
}
