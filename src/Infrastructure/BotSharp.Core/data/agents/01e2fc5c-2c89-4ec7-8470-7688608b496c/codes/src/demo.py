import argparse

def main():
    parser = argparse.ArgumentParser(description="Receive named arguments")
    parser.add_argument("--first_name", required=True, help="The first name")
    parser.add_argument("--last_name", required=True, help="The last name")

    args = parser.parse_args()
    print(f"Hello, {args.first_name} {args.last_name}!")

if __name__ == "__main__":
    main()