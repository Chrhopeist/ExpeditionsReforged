import json
import pandas as pd
from pathlib import Path

# ================= CONFIG =================

EXCEL_PATH = Path("Expeditions_Data_MultiTab.xlsx")
OUTPUT_PATH = Path("expeditions.json")

# ==========================================

def load_sheet(sheet_name: str) -> pd.DataFrame:
    df = pd.read_excel(EXCEL_PATH, sheet_name=sheet_name, header=1)

    df.columns = (
        df.columns.astype(str)
        .str.strip()
        .str.replace("\ufeff", "")
    )

    # Important: blanks become empty strings, NOT NaN
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
        expedition_id = row.get("id", "")
        if expedition_id == "":
            continue

        expeditions[expedition_id] = {
            "id": expedition_id,
            "displayNameKey": row.get("displayNameKey", ""),
            "descriptionKey": row.get("descriptionKey", ""),
            "category": row.get("category", ""),
            "rarity": int(row.get("rarity", 1)),
            "durationTicks": int(row.get("durationTicks", 1)),
            "difficulty": int(row.get("difficulty", 1)),
            "minProgressionTier": row.get("minProgressionTier", ""),
            "isRepeatable": bool(row.get("isRepeatable", False)),
            "isDailyEligible": bool(row.get("isDailyEligible", False)),
            "questGiverNpcId": int(row["npcHeadID"]) if row.get("npcHeadID", "") != "" else None,
            "prerequisites": [],
            "deliverables": [],
            "rewards": [],
            "dailyRewards": []
        }

    # ---------- QuestConditionsForCompletion → prerequisites ----------
    for _, row in conditions.iterrows():
        expedition_id = row.get("expeditionId", "")
        if expedition_id == "" or expedition_id not in expeditions:
            continue

        condition_type = row.get("Type", "")
        if condition_type == "":
            continue

        target = row.get("target", "")

        expeditions[expedition_id]["prerequisites"].append({
            "id": build_condition_id(condition_type, target),
            "type": condition_type,
            "target": target,
            "requiredCount": int(row.get("requiredCount", 0)),
            "description": row.get("description", "")
        })

    # ---------- QuestDeliverables ----------
    for _, row in deliverables.iterrows():
        expedition_id = row.get("expeditionId", "")
        if expedition_id == "" or expedition_id not in expeditions:
            continue

        item_id = row.get("ItemId", "")
        if item_id == "":
            continue  # blank means no deliverable

        expeditions[expedition_id]["deliverables"].append({
            "id": int(item_id),
            "requiredCount": int(row.get("requiredCount", 0)),
            "consumesItems": bool(row.get("consumesItems", False)),
            "description": row.get("description", "")
        })

    # ---------- QuestRewards ----------
    for _, row in rewards.iterrows():
        expedition_id = row.get("expeditionId", "")
        if expedition_id == "" or expedition_id not in expeditions:
            continue

        item_id = row.get("itemId", "")
        if item_id == "":
            continue  # blank means no reward

        reward = {
            "id": int(item_id),
            "minStack": int(row.get("minStack", 1)),
            "maxStack": int(row.get("maxStack", 1)),
            "dropChance": float(row.get("dropChance", 1.0))
        }

        # Daily rewards supported structurally
        if bool(row.get("isDailyReward", False)):
            expeditions[expedition_id]["dailyRewards"].append(reward)
        else:
            expeditions[expedition_id]["rewards"].append(reward)

    return list(expeditions.values())


if __name__ == "__main__":
    data = export_expeditions()

    with open(OUTPUT_PATH, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)

    print(f"Exported {len(data)} expeditions to {OUTPUT_PATH.resolve()}")
