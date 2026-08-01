# Security Configuration Guide

## Environment Variables Setup

This application requires AWS Bedrock credentials to function. **Never commit actual credentials to git.**

### Setup Instructions:

1. Copy the example environment file:
   ```bash
   cp HospitalityAI.Api/.env.example HospitalityAI.Api/.env
   ```

2. Edit `.env` with your actual values:
   - Replace `your_actual_aws_access_key_here` with your AWS Access Key ID
   - Replace `your_actual_aws_secret_key_here` with your AWS Secret Access Key
   - Update the Bedrock agent ARNs with your actual agent IDs
   - Set a secure JWT key (at least 32 characters)

3. Ensure `.env` is in your `.gitignore` (it already is!)

### Alternative: Using AWS Environment Variables

Instead of the configuration file, you can set these environment variables:
- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`
- `AWS_DEFAULT_REGION`

### For Production Deployment:

Use AWS IAM roles instead of access keys when possible. The application will automatically use the default credential chain (IAM roles, environment variables, AWS profiles) if no explicit credentials are configured.

## Files That Should Never Be Committed:

- `HospitalityAI.Api/.env`
- Any file containing actual AWS credentials
- Any file containing production database connection strings
- Any file containing API keys or tokens

## Before Pushing to Git:

1. Verify no actual credentials are in any committed files
2. Check that `.env` files are properly ignored
3. Ensure placeholder values are used in configuration templates

## Emergency: If Secrets Are Accidentally Committed:

1. **Immediately rotate** all exposed credentials in AWS
2. Use `git filter-branch` or BFG Repo-Cleaner to remove secrets from git history
3. Force push the cleaned history (coordinate with team first)
4. Notify your security team