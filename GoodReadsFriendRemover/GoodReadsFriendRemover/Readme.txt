GoodReads has a tendency to spam people when new books are added.

Turns out, if you linked GoodReads to your Facebook profile, and merged in
500+ people, you probably don't care about everyone's book suggestions!

Unfortunately, the user flow for deleting users is horrible.  You have to
click 'Edit' at the top of the page and then the little 'X' by each person's
name one at a time.  The page reloads, and you can start the process over.

I wrote a little console app that allows you to (V1) pop in your Cookie and
Session ID information, so that it can automate much of this process on your
behalf.

The tool will scrape all of your friend pages until it can't find any more, and
then will ask you, sequentially, if you want to remove each person.  Only when
you say "Y" twice in a row, will it attempt to delete a person.  "N" will move
to the next person.