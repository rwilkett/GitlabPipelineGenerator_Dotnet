# CI/CD Variable Copy Feature

## Overview

This feature adds the ability to copy CI/CD variables between GitLab projects when comparing them in the web application. When comparing two projects and there are variables in the source project that don't exist in the target project, users can now easily copy those missing variables.

## Features

### Individual Variable Copy
- **Copy Variable Button**: For each variable that exists only in the source project, a "Copy Variable" button appears in the Action column
- **One-Click Copy**: Click the button to copy that specific variable from source to target project
- **Preserves All Properties**: Copies the variable with all its properties including:
  - Variable type (env_var, file)
  - Protected status
  - Masked status
  - Environment scope
  - Description

### Bulk Variable Copy
- **Copy All Missing Variables Button**: A prominent button at the top of the Variable Differences section
- **Batch Operation**: Copies all variables that exist only in the source project in a single operation
- **Progress Indication**: Shows loading spinner and status during the copy operation

## User Interface

### Visual Indicators
- **Highlighted Rows**: Rows with "Source Only" variables are highlighted in light blue when project comparison is active
- **Action Buttons**: 
  - Individual "Copy Variable" buttons for single variable operations
  - "Copy All Missing Variables" button for bulk operations
- **Loading States**: Both individual and bulk operations show loading spinners during execution

### When Available
The copy functionality is available when:
- Both source and target instances are selected
- Both source and target projects are specified (not groups)
- Resource type is set to "project" for both source and target
- There are variables in the source that don't exist in the target

## Technical Implementation

### Backend Changes
- **GitLabClient.CreateProjectVariableAsync()**: New method to create project variables via GitLab API
- **Enhanced API Support**: Full support for all GitLab project variable properties

### Frontend Changes
- **Compare.razor**: Enhanced with variable copying functionality
- **Conditional UI**: Copy buttons only appear when appropriate conditions are met
- **Error Handling**: Proper error messages and loading states

## Usage Example

1. **Setup Comparison**:
   - Select source GitLab instance and project
   - Select target GitLab instance and project
   - Set both resource types to "project"
   - Click "Compare"

2. **Copy Variables**:
   - Review the Variable Differences table
   - For individual variables: Click "Copy Variable" button next to any "Source Only" variable
   - For bulk copy: Click "Copy All Missing Variables" button at the top

3. **Verification**:
   - The comparison automatically refreshes after copying
   - Previously "Source Only" variables should now show as "Same" status

## Security Considerations

- **Authentication**: Uses existing GitLab API tokens for authentication
- **Permissions**: Requires appropriate permissions on target project to create variables
- **Variable Values**: Preserves masked/protected status of sensitive variables
- **Audit Trail**: All variable creation operations are logged in GitLab's audit logs

## Error Handling

- **Permission Errors**: Clear error messages when user lacks permissions
- **Network Issues**: Graceful handling of API failures
- **Validation**: Prevents copying when required fields are missing
- **Rollback**: Individual variable failures don't affect other operations in bulk copy

## Benefits

- **Time Saving**: Eliminates manual variable recreation between projects
- **Accuracy**: Preserves all variable properties and reduces human error
- **Convenience**: One-click operations for both individual and bulk copying
- **Visibility**: Clear indication of what variables will be copied before action