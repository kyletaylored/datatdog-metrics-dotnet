# Publishing to NuGet

This document describes how to publish packages to NuGet.org using the automated workflow.

## Quick Start

When you publish a GitHub release, the package is **automatically** published to NuGet.org:

```bash
# Create and publish a release
gh release create v0.1.0 --title "v0.1.0" --notes "Release notes here"
```

The workflow will automatically:
- ✅ Download the packages from the release
- ✅ Validate checksums
- ✅ Publish to NuGet.org
- ✅ Publish to GitHub Packages

**That's it!** No manual steps required.

## Prerequisites

Before publishing, ensure:

1. ✅ Version has been bumped in `.csproj` files
2. ✅ Tests are passing
3. ✅ Release has been created with packages attached
4. ✅ Tag follows format: `vX.Y.Z` (e.g., `v0.1.0`)
5. ✅ `RELEASE_TOKEN` secret is configured (required for automatic publishing)

## How It Works

### Automatic Publishing

The workflow is triggered automatically when you publish a GitHub release:

1. **Tag Validation** - Verifies the release tag exists
2. **Download Packages** - Downloads `.nupkg` files from the release
3. **Verify Checksums** - Validates package integrity
4. **Package Validation** - Checks package structure and metadata
5. **Publish to NuGet.org** - Pushes to public NuGet feed
6. **Publish to GitHub Packages** - Pushes to GitHub's package registry

### Validation

The workflow will **fail** if:
- ❌ Tag doesn't exist in the repository
- ❌ Tag format is invalid (should be `vX.Y.Z`)
- ❌ Release doesn't exist for the tag
- ❌ Checksums don't match
- ❌ Package structure is invalid

### What Gets Published

The workflow publishes to two locations:

1. **NuGet.org** (public) - Primary package repository
2. **GitHub Packages** (organization) - Backup/internal use

## Publishing a New Version

### Step-by-Step

```bash
# 1. Update version in .csproj files
# (usually done in a PR and merged to main)

# 2. Create git tag
git tag v0.1.0
git push origin v0.1.0

# 3. Create GitHub release (this triggers automatic publish)
gh release create v0.1.0 \
  --title "v0.1.0" \
  --notes "Release notes" \
  ./packages/*.nupkg \
  ./packages/checksums.txt
```

**Done!** The workflow automatically publishes to NuGet.org within minutes.

### Republishing After Failure

If the workflow fails and you need to retry:

```bash
# 1. Fix the issue (e.g., update packages, fix checksums)

# 2. Update the release with new packages
gh release upload v0.1.0 ./packages/*.nupkg --clobber
gh release upload v0.1.0 ./packages/checksums.txt --clobber

# 3. Re-trigger the workflow
gh workflow run publish-to-nuget.yml --ref v0.1.0
```

**Note:** The `--skip-duplicate` flag prevents errors if version already exists on NuGet.

## Troubleshooting

### "Tag does not exist"

**Problem:** You entered a tag that doesn't exist.

**Solution:**
```bash
# Check available tags
git tag --sort=-version:refname

# Or create the tag
git tag v0.1.0
git push origin v0.1.0
```

### "Release not found"

**Problem:** No GitHub release exists for the tag.

**Solution:**
```bash
# Create release with packages
gh release create v0.1.0 ./packages/*.nupkg
```

### "Packages already exist"

**Problem:** You're trying to republish the same version.

**Result:** NuGet will skip duplicates (this is fine).

**Solution:** If you need to publish changes:
1. Increment the version
2. Create a new tag and release

### "Checksums verification failed"

**Problem:** Package files don't match checksums.

**Solution:** Rebuild and recreate the release:
```bash
# Rebuild
dotnet build --configuration Release

# Generate new checksums
cd ./packages
sha256sum *.nupkg > checksums.txt

# Recreate release
gh release delete v0.1.0 --yes
gh release create v0.1.0 ./packages/*.nupkg ./packages/checksums.txt
```

## Security Notes

- 🔒 **NuGet API Key** is stored in GitHub Secrets as `NUGET_API_KEY`
- 🔒 **Release Token** is stored in GitHub Secrets as `RELEASE_TOKEN` (Personal Access Token with `repo` scope)
  - **Required** to trigger the publish workflow when a release is created by the build workflow
  - GitHub Actions workflows triggered by `GITHUB_TOKEN` cannot trigger other workflows (security feature)
  - Create at: https://github.com/settings/tokens (Classic Token with `repo` scope)
  - Add to repository secrets: Settings → Secrets and variables → Actions → New repository secret
  - Without this token, you'll need to manually trigger the publish workflow after each release
- 🔒 Publish jobs require **environment approval** for production

## Package Availability

After successful publish:

- **NuGet.org**: Available in **15-30 minutes** (indexing time)
- **GitHub Packages**: Available **immediately**

Check package status:
- [NuGet Profile](https://www.nuget.org/profiles/kyletaylored)
- [GitHub Packages](https://github.com/kyletaylored?tab=packages)

## Links

- [NuGet Package](https://www.nuget.org/packages/kyletaylored.Datadog.Metrics/)
- [Publish Workflow](.github/workflows/publish-to-nuget.yml)
- [Build Workflow](.github/workflows/build-release.yml)
