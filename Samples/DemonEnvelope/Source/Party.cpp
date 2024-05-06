#include "Party.h"
#include "Item.h"

using namespace DE;

//==========================
//Party Stat Curve
//==========================

constexpr int LEVEL_CAP = 60;

int Party_Stat_Curve::Curve0(int level) const
{
    // Quadratic ease in (Medium growth)
    const float a = float(level) / float(LEVEL_CAP);
    return int(a * a * (_max - _min) + _min);
}

int Party_Stat_Curve::Curve1(int level) const
{
    // Quadratic ease in (Fast growth)
    const float a = float(level) / float(LEVEL_CAP);
    return int(std::powf(a, 1.8f) * (_max - _min) + _min);
}

int Party_Stat_Curve::Curve2(int level) const
{
    // Quadratic ease in (Slow growth)
    const float a = float(level) / float(LEVEL_CAP);
    return int(std::powf(a, 2.3f) * (_max - _min) + _min);
}

int Party_Stat_Curve::Calculate(int level) const
{
    int value = 0;
    switch (_curveType)
    {
    case 0:
        value = Curve0(level);
        break;
    case 1:
        value = Curve1(level);
        break;
    case 2:
        value = Curve2(level);
        break;
    }

    return value;
}

//=========================
//Party Member Stats
//=========================

void Party_Member_Stats::Reset()
{
    _attack = 0;
    _defence = 0;
    _maxHP = 0;
    _maxDP = 0;
}

//===============
//Party Member
//===============

Party_Member::Party_Member(const std::string& name, int id, int level)
    :
    _name(name),
    _id(id),
    _level(level),
    _exp(0),
    _active(false),
    _helm(-1),
    _chest(-1),
    _footwear(-1),
    _legs(-1),
    _gloves(-1),
    _weaponPrimary(-1),
    _weaponSecondary(-1),
    _health(0),
    _dp(0)
{
}

void Party_Member::CalculateStats(const Party_Pack& pack, const std::shared_ptr<ItemConfig>& cfg)
{
    _stats.Reset();

    std::vector<int> items;
    items.push_back(_gloves);
    items.push_back(_helm);
    items.push_back(_chest);
    items.push_back(_footwear);
    items.push_back(_legs);
    items.push_back(_weaponPrimary);
    items.push_back(_weaponSecondary);

    int defence = _stats.DefenceCurve().Calculate(_level);
    int attack = _stats.AttackCurve().Calculate(_level);
    int hp = _stats.MaxHPCurve().Calculate(_level);

    for (int id : items)
    {
        if (id > -1)
        {
            const auto& item = cfg->Get(id);
            defence += item.GetIntegerAttribute("Defence");
            attack += item.GetIntegerAttribute("Attack");
        }
    }

    _stats.SetAttack(attack);
    _stats.SetDefence(defence);
    _stats.SetMaxHP(hp);
}

//==================================
// Party_Pack
//==================================

void Party_Pack::AddItem(int id, int quantity)
{
    if (quantity > 0)
    {
        bool found = false;

        const auto& item = _items.find(id);
        if (item != _items.end())
        {
            item->second.Increment(quantity);
            found = true;
        }

        if (!found)
        {
            Party_Item item(id, quantity);
            _items.insert(std::pair<int, Party_Item>(id, item));
            _ids.push_back(id);
        }
    }
}

void Party_Pack::RemoveItem(int id, int quantity)
{
    if (quantity > 0)
    {
        const auto& item = _items.find(id);
        if (item != _items.end())
        {
            item->second.Decrement(quantity);

            if (item->second.NumItems() == 0)
            {
                _ids.erase(std::find(_ids.begin(), _ids.end(), item->first));
                _items.erase(item);
            }
        }
    }
}

bool Party_Pack::Equip(int id)
{
    const auto& item = _items.find(id);
    if (item != _items.end())
    {
        return item->second.SetEquipped(1);
    }

    return false;
}

bool Party_Pack::Unequip(int id)
{
    const auto& item = _items.find(id);
    if (item != _items.end())
    {
        return item->second.Unequip(1);
    }

    return false;
}

//===================
//Party
//===================

Party::Party() :
    _unknown("unknown", -1, 0)
{
    const int simon = AddMember(Party_Member("Simon", 0, 5));
    const int emily = AddMember(Party_Member("Emily", 1, 5));
    const int julius = AddMember(Party_Member("Julius", 2, 5));
    const int scarlet = AddMember(Party_Member("Scarlet", 3, 5));

    _members[simon].SetActive(true);
    _members[simon].GetStats().SetAttackCurve(Party_Stat_Curve(0, 20, 280));
    _members[simon].GetStats().SetDefenceCurve(Party_Stat_Curve(0, 10, 230));
    _members[simon].GetStats().SetMaxHPCurve(Party_Stat_Curve(1, 40, 400));

    _members[emily].SetActive(true);
    _members[emily].GetStats().SetAttackCurve(Party_Stat_Curve(1, 30, 180));
    _members[emily].GetStats().SetDefenceCurve(Party_Stat_Curve(0, 15, 320));
    _members[emily].GetStats().SetMaxHPCurve(Party_Stat_Curve(2, 40, 470));

    _members[julius].SetActive(true);
    _members[julius].GetStats().SetAttackCurve(Party_Stat_Curve(1, 25, 200));
    _members[julius].GetStats().SetDefenceCurve(Party_Stat_Curve(1, 32, 200));
    _members[julius].GetStats().SetMaxHPCurve(Party_Stat_Curve(0, 38, 410));

    _members[scarlet].SetActive(true);
    _members[scarlet].GetStats().SetAttackCurve(Party_Stat_Curve(2, 35, 300));
    _members[scarlet].GetStats().SetDefenceCurve(Party_Stat_Curve(1, 20, 240));
    _members[scarlet].GetStats().SetMaxHPCurve(Party_Stat_Curve(0, 47, 390));

    _pack.AddItem(100, 5);
    _pack.AddItem(101, 2);
    _pack.AddItem(0, 1);
    _pack.AddItem(1, 1);
    _pack.AddItem(200, 1);
    _pack.AddItem(300, 1);
    _pack.AddItem(400, 1);
    _pack.AddItem(500, 1);
    _pack.AddItem(600, 1);
}

Party_Member& Party::FindMemberByID(int id)
{
    for (auto& member : _members)
    {
        if (member.ID() == id)
        {
            return member;
        }
    }

    return _unknown;
}

const Party_Member& Party::FindMemberByID(int id) const
{
    for (auto& member : _members)
    {
        if (member.ID() == id)
        {
            return member;
        }
    }

    return _unknown;
}

int Party::AddMember(const Party_Member& member)
{
    _members.push_back(member);
    return int(_members.size()) - 1;
}
