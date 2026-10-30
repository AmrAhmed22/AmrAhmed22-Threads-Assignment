Problems:

1- Thread Sleep

2- TablesLeft-- and Booked++ are not atomic,its not thread safe
it needs a lock


before:
Booked=33, TablesLeft=-28
Booked=37, TablesLeft=-32

After(Always like that now):
Booked=5, TablesLeft=0 

