using System;
using Npgsql;
using var conn = new NpgsqlConnection("Host=aws-1-ap-southeast-1.pooler.supabase.com;Database=postgres;Username=postgres.gxzjcobrhhgijvveazxa;Password=SlcCouvFr1WiCUak;Port=6543;SSL Mode=Require;Trust Server Certificate=true;Pooling=false;Max Auto Prepare=0;");
conn.Open();
using var cmd = new NpgsqlCommand("SELECT \"Id\", \"AssetName\", \"NextMaintenanceDueDate\" FROM \"Assets\" WHERE \"AssetType\" = 'Elevator' LIMIT 10", conn);
using var reader = cmd.ExecuteReader();
while(reader.Read()){
    Console.WriteLine($"{reader[0]} | {reader[1]} | {reader[2]}");
}
