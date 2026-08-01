import json

DATASET = {
    "occupancyByDayOfWeek": {
        "Monday": 62, "Tuesday": 58, "Wednesday": 64, "Thursday": 70,
        "Friday": 85, "Saturday": 91, "Sunday": 78
    },
    "taskVolumeByTypeAndDay": {
        "Housekeeping": {
            "Monday": 24, "Tuesday": 22, "Wednesday": 25, "Thursday": 28,
            "Friday": 35, "Saturday": 40, "Sunday": 32
        },
        "Maintenance": {
            "Monday": 6, "Tuesday": 5, "Wednesday": 7, "Thursday": 6,
            "Friday": 8, "Saturday": 9, "Sunday": 7
        },
        "GuestRequest": {
            "Monday": 12, "Tuesday": 10, "Wednesday": 13, "Thursday": 15,
            "Friday": 22, "Saturday": 28, "Sunday": 20
        },
        "RoomService": {
            "Monday": 18, "Tuesday": 16, "Wednesday": 19, "Thursday": 22,
            "Friday": 30, "Saturday": 38, "Sunday": 26
        }
    },
    "inventoryConsumptionRatePerRoom": {
        "bathTowelLinenSets": 2.1,
        "toiletryKits": 1.0,
        "minibarStockUnits": 3.5,
        "cleaningSupplyKits": 0.8,
        "breakfastCoverSets": 0.9
    },
    "currentInventoryLevels": {
        "bathTowelLinenSets": 180,
        "toiletryKits": 120,
        "minibarStockUnits": 95,
        "cleaningSupplyKits": 60,
        "breakfastCoverSets": 110
    },
    "seasonalAdjustments": {
        "Summer": 1.25,
        "December": 1.35,
        "Default": 1.0
    }
}


def lambda_handler(event, context):
    action = event.get("actionGroup", "")
    fn     = event.get("function", "")

    return {
        "messageVersion": "1.0",
        "response": {
            "actionGroup": action,
            "function": fn,
            "functionResponse": {
                "responseBody": {
                    "TEXT": {
                        "body": json.dumps({
                            "found": True,
                            "data": DATASET
                        })
                    }
                }
            }
        }
    }
