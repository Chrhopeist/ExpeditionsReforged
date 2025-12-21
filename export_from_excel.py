import json
import pandas as pd
from pathlib import Path

# ================= CONFIG =================

EXCEL_PATH = Path("Expeditions_Data_MultiTab.xlsx")
OUTPUT_PATH = Path("expeditions.json")

# ==========================================


def parse_bool(value) -> bool:
    if isinstance(value, bool):
        return value
    if isinstance(value, (int, float)):
        return value != 0
    if isinstance(value, str):
        return value.strip().lower() in ("true", "1", "yes", "y")
    return False


def load_sheet(sheet_name: str) -> pd.DataFrame:
    df = pd.read_excel(EXCEL_PATH, sheet_name=sheet_name, header=1)

    df.columns = (
        df.columns.astype(str)
        .str.strip()
        .str.replace("\ufeff", "")
    )

    return df.fillna("")


def build_condition_id(condition_type: str, target: str) -> str:
    return f"{condition_type}_{target}".lower()


def export_expeditions():
    props = load_sheet("QuestProperties")
    conditions = load_sheet("QuestConditionsForCompletion")
    deliverables = load_sheet("QuestDeliverables")
    rewards = load_sheet("QuestRewards")

    expeditions: dict[str, dict] = {}

    # ---------- QuestProperties ----------
    for _, row in props.iterrows():
        expedition_id = str(row.get("id", "")).strip()
        if not expedition_id:
            continue

        display_name_key = str(row.get("displayNameKey", "")).strip()
        description_key = str(row.get("descriptionKey", "")).strip()
        category = str(row.get("category", "")).strip()

        if not display_name_key or not description_key or not category:
            print(
                f"[ERROR] Skipping expedition '{expedition_id}': "
                f"missing displayNameKey / descriptionKey / category"
            )
            continue

        # Quest giver NPCID (authoritative)
        try:
            quest_giver_npc_id = int(row.get("questGiverNPCID", 0))
        except Exception:
            quest_giver_npc_id = 0

        # Numeric progression tier ONLY
        try:
            min_progression_tier = int(row.get("minProgressionTierID", 1))
        except Exception:
            min_progression_tier = 1

        expeditions[expedition_id] = {
            "id": expedition_id,
            "displayNameKey": display_name_key,
            "descriptionKey": description_key,
            "category": category,
            "rarity": int(row.get("rarity", 1) or 1),
            "durationTicks": int(row.get("durationTicks", 1) or 1),
            "difficulty": int(row.get("difficulty", 1) or 1),
            "minProgressionTier": str(min_progression_tier),
            "isRepeatable": parse_bool(row.get("isRepeatable", False)),
            "isDailyEligible": parse_bool(row.get("isDailyEligible", False)),
            # IMPORTANT: this is NPCID, not a head index
            "questGiverNpcId": quest_giver_npc_id,
            "prerequisites": [],
            "deliverables": [],
            "rewards": [],
            "dailyRewards": []
        }

    # ---------- QuestConditionsForCompletion ----------
    for _, row in conditions.iterrows():
        expedition_id = str(row.get("expeditionId", "")).strip()
        if expedition_id not in expeditions:
            continue

        condition_type = str(row.get("Type", "")).strip()
        if not condition_type:
            continue

        target = str(row.get("target", "")).strip()

        expeditions[expedition_id]["prerequisites"].append({
            "id": build_condition_id(condition_type, target),
            "requiredCount": int(row.get("requiredCount", 0) or 0),
            "description": str(row.get("description", "")).strip()
        })

    # ---------- QuestDeliverables ----------
    for _, row in deliverables.iterrows():
        expedition_id = str(row.get("expeditionId", "")).strip()
        if expedition_id not in expeditions:
            continue

        item_id = row.get("ItemId", "")
        if item_id == "":
            continue

        expeditions[expedition_id]["deliverables"].append({
            "id": str(int(item_id)),
            "requiredCount": int(row.get("requiredCount", 0) or 0),
            "consumesItems": parse_bool(row.get("consumesItems", False)),
            "description": str(row.get("description", "")).strip()
        })

    # ---------- QuestRewards ----------
    for _, row in rewards.iterrows():
        expedition_id = str(row.get("expeditionId", "")).strip()
        if expedition_id not in expeditions:
            continue

        item_id = row.get("itemId", "")
        if item_id == "":
            continue

        reward = {
            "id": str(int(item_id)),
            "minStack": int(row.get("minStack", 1) or 1),
            "maxStack": int(row.get("maxStack", 1) or 1),
            "dropChance": float(row.get("dropChance", 1.0) or 1.0)
        }

        if parse_bool(row.get("isDailyReward", False)):
            expeditions[expedition_id]["dailyRewards"].append(reward)
        else:
            expeditions[expedition_id]["rewards"].append(reward)

    return list(expeditions.values())


if __name__ == "__main__":
    data = export_expeditions()

    with open(OUTPUT_PATH, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

    print(f"Exported {len(data)} expeditions to {OUTPUT_PATH.resolve()}")
