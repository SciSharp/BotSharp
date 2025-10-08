import argparse
import json

def main():
    parser = argparse.ArgumentParser(description="Receive named arguments")
    parser.add_argument("--first_name", required=True, help="The first name")
    parser.add_argument("--last_name", required=True, help="The last name")

    args, _ = parser.parse_known_args()
    obj = {
        "first_name": args.first_name,
        "last_name":args.last_name
    }
    print(f"{json.dumps(obj)}")

if __name__ == "__main__":
    main()