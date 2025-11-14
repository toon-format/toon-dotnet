# generate spec
GH_REPO="https://github.com/toon-format/spec.git"
OUT_DIR="./tests/ToonFormat.Tests"

# build and execute spec generator
dotnet build tests/ToonFormat.SpecGenerator

dotnet run --project tests/ToonFormat.SpecGenerator -- --url="$GH_REPO" --output="$OUT_DIR" --branch="v2.0.0" --loglevel="Information"