$branchSelector = '.*continousStashing$'
$repoSelector = 'C:\git\Adrian.AutoHotKeyUtils'



$stashName = 'continousStashing'

cd $repoSelector
$currentBranch = git branch --show-current
$currentBranch
$branchSelector
$currentBranch -match $branchSelector
if( $currentBranch -match $branchSelector){
    git add .
    git stash push --keep-index -m $stashName

    
    # skip the first 2 so we have a backup
    for ($i=2; $i -le 10; $i++) {
        try{
            $pastStashMessage = git stash list --skip=$i -n 1 --pretty=format:%B    

            if(!$pastStashMessage)
            {
                'run out of stashes to check for deletion, finishing.'
                break
            }
    
            $stashNameSelector = 'On ' + $currentBranch + ': ' + $stashName 

            if( $stashNameSelector -eq $pastStashMessage ){
                $zeroBasedIndex = $i - 1
                git stash drop stash@`{$zeroBasedIndex`}

                # decrease count since the next stash will have been removed from the list, and be in an earlier position.
                # disabled because if an error happens, this loops forever. So, we will get some stashes each time, and eventually probably all of them. 
                #$i = $i - 1;
            }
        }
        catch{
            break
        }
    }

    git stash list
}