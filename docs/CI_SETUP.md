# CI/CD Setup Guide

This project uses [GameCI](https://game.ci/) for Unity testing in GitHub Actions.

## Required GitHub Secrets

You need to configure three secrets in your GitHub repository settings:

1. Go to **Settings → Secrets and variables → Actions**
2. Click **New repository secret** for each:

| Secret | Description |
|--------|-------------|
| `UNITY_LICENSE` | Contents of your Unity license file (`.ulf`) |
| `UNITY_EMAIL` | Your Unity account email |
| `UNITY_PASSWORD` | Your Unity account password |

## Getting Your Unity License File

### Personal License (Free)

1. Install Unity Hub and log in with your Unity account
2. Activate a personal license: **Unity Hub → Preferences → Licenses → Add**
3. Select "Get a free personal license"
4. Find your `.ulf` file:
   - **Windows**: `C:\ProgramData\Unity\Unity_lic.ulf`
   - **macOS**: `/Library/Application Support/Unity/Unity_lic.ulf`
   - **Linux**: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
5. Copy the entire contents of this file into the `UNITY_LICENSE` secret

### Professional License (Plus/Pro)

For Unity Plus/Pro licenses, use your serial key instead:

| Secret | Description |
|--------|-------------|
| `UNITY_SERIAL` | Your serial key (format: `XX-XXXX-XXXX-XXXX-XXXX-XXXX`) |
| `UNITY_EMAIL` | Your Unity account email |
| `UNITY_PASSWORD` | Your Unity account password |

Find your serial key at: https://id.unity.com/en/subscriptions

Then update `.github/workflows/tests.yml` to use `UNITY_SERIAL` instead of `UNITY_LICENSE`.

## What the CI Workflow Does

The workflow (`.github/workflows/tests.yml`) runs on:
- Push to `main` branch
- Pull requests targeting `main`

Steps:
1. **Checkout** - Clones the repository with Git LFS support
2. **Cache** - Caches the Unity Library folder for faster builds
3. **Test** - Runs EditMode tests using GameCI's unity-test-runner
4. **Upload** - Saves test results as artifacts (retained 14 days)

## Running Tests Locally

To run tests locally in Unity:

1. Open the project in Unity 2022.3 LTS
2. Open **Window → General → Test Runner**
3. Select **EditMode** tab
4. Click **Run All**

## Troubleshooting

### Tests fail with license error
- Verify all three secrets are set correctly
- Check that your `.ulf` file is for Unity 2022.3.x
- Ensure your license hasn't expired

### Cache miss every time
- The cache key is based on Assets/Packages/ProjectSettings hashes
- First run after changes will always miss
- Subsequent runs should hit the cache

### PlayMode tests not running
- PlayMode tests are commented out by default
- Uncomment the `playmode` line in the test matrix when PlayMode tests are added

## Security Notes

- GameCI does not store your credentials - they're only used during CI builds
- Secrets are encrypted and not visible in logs
- Use repository secrets, not environment variables in workflow files
