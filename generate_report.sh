#!/bin/bash

if [ ! -d ".git" ]; then
    echo "❌ Error: This script must be executed from the root of your Git repository."
    exit 1
fi

if ! command -v gh &> /dev/null || ! command -v jq &> /dev/null; then
    echo "❌ Error: 'gh' (GitHub CLI) and 'jq' must be installed."
    exit 1
fi

TIMESTAMP=$(date +"%Y-%m-%d_%H-%M-%S")
OUTPUT_DIR="reports"
mkdir -p "$OUTPUT_DIR"

get_max_author_len() {
    local LEN=$(git log --all --no-merges --format="%an" | awk '{ print length }' | sort -rn | head -n1)
    if [[ -z "$LEN" ]]; then echo "15"; else echo "$LEN"; fi
}

generate_report_logic() {
    local TARGET_USER="$1"
    local TARGET_FILE="$2"
    local CONTEXT_REPO="$3" 

    local PAD=$(get_max_author_len)

    (
        echo "Git Report for user: $TARGET_USER"
        if [[ -n "$CONTEXT_REPO" ]]; then
            echo "Repository context: $CONTEXT_REPO"
        else
            echo "Repository context: Current Directory (Auto-detect)"
        fi
        echo "Generated on: $(date)"
        echo "---------------------------------------------------"

        # count commits 
        total_commits=$(git log --all --author="$TARGET_USER" --no-merges --oneline | wc -l | xargs)
        
        # count merged PRs
        if [[ -n "$CONTEXT_REPO" ]]; then
             total_prs_merged=$(gh pr list --repo "$CONTEXT_REPO" --author "$TARGET_USER" --state merged --limit 1000 --json number | jq length)
        else
             total_prs_merged=$(gh pr list --author "$TARGET_USER" --state merged --limit 1000 --json number | jq length)
        fi

        # count approved PRs (reviews)
        SEARCH_QUERY="reviewed-by:$TARGET_USER review:approved"
        if [[ -n "$CONTEXT_REPO" ]]; then
            total_prs_approved=$(gh pr list --repo "$CONTEXT_REPO" --search "$SEARCH_QUERY" --state all --limit 1000 --json number | jq length)
        else
            total_prs_approved=$(gh pr list --search "$SEARCH_QUERY" --state all --limit 1000 --json number | jq length)
        fi
        
        echo "Total commits (excl. merges):       $total_commits"
        echo "Total PRs merged (Author):          $total_prs_merged"
        echo "Total PRs approved (Review):        $total_prs_approved"
        echo ""
        echo "--- Commit Log (Chronological) ---"
        echo "Hash    | Date             | Author$(printf '%*s' $((PAD-6)) '') | Message"
        echo "--------------------------------------------------------------------------------"
        
        # format: hash | date | author | message
        git log --all --author="$TARGET_USER" --no-merges --pretty=format:"%h | %ad | %<($PAD)%an | %s" --date=format:'%Y-%m-%d %H:%M' --reverse
    ) > "$TARGET_FILE"
}

generate_full_repo_report() {
    local TARGET_FILE="$1"
    local CONTEXT_REPO="$2"

    local PAD=$(get_max_author_len)

    (
        echo "FULL REPOSITORY STATS (All Contributors Combined)"
        if [[ -n "$CONTEXT_REPO" ]]; then
            echo "Repository context: $CONTEXT_REPO"
        else
            echo "Repository context: Current Directory (Auto-detect)"
        fi
        echo "Generated on: $(date)"
        echo "---------------------------------------------------"

        # stats
        total_commits=$(git log --all --no-merges --oneline | wc -l | xargs)
        
        if [[ -n "$CONTEXT_REPO" ]]; then
             total_prs_merged=$(gh pr list --repo "$CONTEXT_REPO" --state merged --limit 1000 --json number | jq length)
             SEARCH_QUERY="review:approved"
             total_prs_approved=$(gh pr list --repo "$CONTEXT_REPO" --search "$SEARCH_QUERY" --state all --limit 1000 --json number | jq length)
        else
             total_prs_merged=$(gh pr list --state merged --limit 1000 --json number | jq length)
             SEARCH_QUERY="review:approved"
             total_prs_approved=$(gh pr list --search "$SEARCH_QUERY" --state all --limit 1000 --json number | jq length)
        fi
        
        echo "Total Project Commits:              $total_commits"
        echo "Total Project PRs Merged:           $total_prs_merged"
        echo "Total Project PRs Approved:         $total_prs_approved"
        echo ""
        echo "--- Full Project History (Chronological) ---"
        
        # header dynamically padded
        echo "Hash    | Date             | Author$(printf '%*s' $((PAD-6)) '') | Message"
        echo "--------------------------------------------------------------------------------"
        
        # format: hash | date | author | message
        git log --all --no-merges --pretty=format:"%h | %ad | %<($PAD)%an | %s" --date=format:'%Y-%m-%d %H:%M' --reverse
    ) > "$TARGET_FILE"
}

echo "--- 📊 Git Contribution Generator ---"
echo "What would you like to do?"
echo "  1) Generate report for a specific person"
echo "  2) Generate reports for the team + Full Repository Report"
echo ""
read -p "Make a choice [1 or 2]: " CHOICE

if [[ "$CHOICE" == "1" ]]; then
    # individual
    echo ""
    read -p "Enter GitHub username (e.g. fayece): " SINGLE_USER

    if [[ -z "$SINGLE_USER" ]]; then
        echo "❌ Error: Name cannot be empty."
        exit 1
    fi

    OUTPUT_FILE="$OUTPUT_DIR/report_${SINGLE_USER}_${TIMESTAMP}.log"
    echo "⏳ Generating..."
    
    REPO_NAME=$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null)
    generate_report_logic "$SINGLE_USER" "$OUTPUT_FILE" "$REPO_NAME"
    
    echo "✅ Done! File saved as: $OUTPUT_FILE"

elif [[ "$CHOICE" == "2" ]]; then
    # team + full repo
    echo ""
    echo "🔍 Retrieving repository data..."
    
    REPO_STRING=$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null)

    if [[ -z "$REPO_STRING" ]]; then
        echo "⚠️  Could not automatically detect repository."
        read -p "Enter the full repo name (e.g. HamidOmarr/CloudChasers_MobyPark): " REPO_STRING
    fi
    
    if [[ -z "$REPO_STRING" ]]; then
         echo "❌ Error: No repository specified."
         exit 1
    fi
    
    echo "🎯 Repository set to: $REPO_STRING"
    echo "🔍 Fetching team members via API..."

    CONTRIBUTORS=$(gh api "repos/$REPO_STRING/contributors" --paginate -q '.[].login' 2>/dev/null)

    if [[ -z "$CONTRIBUTORS" ]]; then
        echo "❌ Error: Could not find contributors."
        exit 1
    fi

    echo "👥 Contributors found: $(echo $CONTRIBUTORS | xargs)"
    echo "📂 Files will be saved in directory: '$OUTPUT_DIR/'"
    echo "---------------------------------------------------"

    # generate individual reports
    for USER in $CONTRIBUTORS; do
        FILE="$OUTPUT_DIR/report_${USER}_${TIMESTAMP}.log"
        echo "   Generating report for: $USER..."
        generate_report_logic "$USER" "$FILE" "$REPO_STRING"
    done

    # generate full repository report
    FULL_REPO_FILE="$OUTPUT_DIR/report_FULL_REPOSITORY_${TIMESTAMP}.log"
    echo "   Generating full repository statistics..."
    generate_full_repo_report "$FULL_REPO_FILE" "$REPO_STRING"

    echo ""
    echo "✅ All reports (Team + Full Repo) have been generated in '$OUTPUT_DIR/'"

else
    echo "❌ Invalid choice."
    exit 1
fi