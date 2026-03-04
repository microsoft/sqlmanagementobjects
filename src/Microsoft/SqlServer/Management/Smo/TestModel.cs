using System;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using  Microsoft.SqlServer.Management.Data.Designer;

class Test
{
    static void Main()
    {
        // Create model object based off existing SMO object
        ITableModel it1 = ModelFactory.CreateTable( new Table() );
        it1.Name = "Table1";
        it1.AnsiNullsStatus = false;

        // Create model object -- the underlying SMO object is created 
        ITableModel it2 = ModelFactory.CreateTable();
        it2.Name = "Table2";
        it2.AnsiNullsStatus = true;

        IDatabaseModel id = ModelFactory.CreateDatabase( new Database() );

        // Add table to collections
        id.Tables.Add(it1);
        id.Tables.Add(it2);

        // Now read the collections

        // Try Count
        Console.WriteLine("The database has {0} tables", id.Tables.Count);

        // Try random access
        Console.WriteLine( id.Tables[0].Name );
        Console.WriteLine( id.Tables[1].Name );

        // Access using enumerator
        foreach( ITableModel t in id.Tables )
        {
            Console.WriteLine( t.Name );
        }

        Console.WriteLine( id.Tables[0].Columns.Count );    // 0
        id.Tables[0].Columns.Add( ModelFactory.CreateColumn() );
        Console.WriteLine( id.Tables[0].Columns.Count );    // 1
    }
}
