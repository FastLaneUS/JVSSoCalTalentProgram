﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using DroneFactory.Models;

namespace DroneFactory.ConnectedLayer
{
    public class InventoryDAL
    {
        // This member will be used by all methods.
        private SqlConnection _sqlConnection = null;
        public void OpenConnection(string connectionString)
        {
            _sqlConnection = new SqlConnection { ConnectionString = connectionString };
            _sqlConnection.Open();
        }

        public void CloseConnection()
        {
            _sqlConnection.Close();
        }
        //public void InsertAuto(string color, string make, string petName)
        //{
        //	// Format and execute SQL statement.
        //	string sql = "Insert Into Inventory" +
        //	  $"(Make, Color, PetName) Values ('{make}', '{color}', '{petName}')";

        //	// Execute using our connection.
        //	using (SqlCommand command = new SqlCommand(sql, _sqlConnection))
        //	{
        //		command.ExecuteNonQuery();
        //	}
        //}
        public void InsertAuto(NewDrone drone)
        {
            // Format and execute SQL statement.
            string sql = "Insert Into Inventory (Make, Color, PetName) Values" +
                $"('{drone.Make}', '{drone.Color}', '{drone.PetName}')";

            // Execute using our connection.
            using (SqlCommand command = new SqlCommand(sql, _sqlConnection))
            {
                command.ExecuteNonQuery();
            }
        }
        public void InsertAuto(string color, string make, string petName)
        {
            // Note the "placeholders" in the SQL query.
            string sql = "Insert Into Inventory" +
                            "(Make, Color, PetName) Values" +
                            "(@Make, @Color, @PetName)";

            // This command will have internal parameters.
            using (SqlCommand command = new SqlCommand(sql, _sqlConnection))
            {
                // Fill params collection.
                SqlParameter parameter = new SqlParameter
                {
                    ParameterName = "@Make",
                    Value = make,
                    SqlDbType = SqlDbType.Char,
                    Size = 10
                };
                command.Parameters.Add(parameter);

                parameter = new SqlParameter
                {
                    ParameterName = "@Color",
                    Value = color,
                    SqlDbType = SqlDbType.Char,
                    Size = 10
                };
                command.Parameters.Add(parameter);

                parameter = new SqlParameter
                {
                    ParameterName = "@PetName",
                    Value = petName,
                    SqlDbType = SqlDbType.Char,
                    Size = 10
                };
                command.Parameters.Add(parameter);

                command.ExecuteNonQuery();
            }
        }

        public void Deletedrone(int id)
        {
            // Get ID of drone to delete, then do so.
            string sql = $"Delete from Inventory where DroneId = '{id}'";
            using (SqlCommand cmd = new SqlCommand(sql, _sqlConnection))
            {
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception("Sorry! That drone is on order!", ex);
                    throw error;
                }
            }
        }
        public void UpdatedronePetName(int id, string newPetName)
        {
            // Get ID of drone to modify and new pet name.
            string sql = $"Update Inventory Set PetName = '{newPetName}' Where DroneId = '{id}'";
            using (SqlCommand command = new SqlCommand(sql, _sqlConnection))
            {
                command.ExecuteNonQuery();
            }
        }
        public List<NewDrone> GetAllInventoryAsList()
        {
            // This will hold the records.
            List<NewDrone> inv = new List<NewDrone>();

            // Prep command object.
            string sql = "Select * From Inventory";
            using (SqlCommand command = new SqlCommand(sql, _sqlConnection))
            {
                SqlDataReader dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    inv.Add(new NewDrone
                    {
                        DroneId = (int)dataReader["DroneId"],
                        Color = (string)dataReader["Color"],
                        Make = (string)dataReader["Make"],
                        PetName = (string)dataReader["PetName"]
                    });
                }
                dataReader.Close();
            }
            return inv;
        }
        public DataTable GetAllInventoryAsDataTable()
        {
            // This will hold the records.
            DataTable dataTable = new DataTable();

            // Prep command object.
            string sql = "Select * From Inventory";
            using (SqlCommand cmd = new SqlCommand(sql, _sqlConnection))
            {
                SqlDataReader dataReader = cmd.ExecuteReader();
                // Fill the DataTable with data from the reader and clean up.
                dataTable.Load(dataReader);
                dataReader.Close();
            }
            return dataTable;
        }
        public string LookUpPetName(int droneId)
        {
            string dronePetName;

            // Establish name of stored proc.
            using (SqlCommand command = new SqlCommand("GetPetName", _sqlConnection))
            {
                command.CommandType = CommandType.StoredProcedure;

                // Input param.
                SqlParameter param = new SqlParameter
                {
                    ParameterName = "@droneId",
                    SqlDbType = SqlDbType.Int,
                    Value = droneId,
                    Direction = ParameterDirection.Input
                };
                command.Parameters.Add(param);

                // Output param.
                param = new SqlParameter
                {
                    ParameterName = "@petName",
                    SqlDbType = SqlDbType.Char,
                    Size = 10,
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(param);

                // Execute the stored proc.
                command.ExecuteNonQuery();

                // Return output param.
                dronePetName = (string)command.Parameters["@petName"].Value;
            }
            return dronePetName;
        }
        public void ProcessCreditRisk(bool throwEx, int custId)
        {
            // First, look up current name based on customer ID.
            string fName;
            string lName;
            var cmdSelect =
                new SqlCommand($"Select * from Customers where CustId = {custId}",
                _sqlConnection);
            using (var dataReader = cmdSelect.ExecuteReader())
            {
                if (dataReader.HasRows)
                {
                    dataReader.Read();
                    fName = (string)dataReader["FirstName"];
                    lName = (string)dataReader["LastName"];
                }
                else
                {
                    return;
                }
            }

            // Create command objects that represent each step of the operation.
            var cmdRemove =
                new SqlCommand($"Delete from Customers where CustId = {custId}",
                _sqlConnection);

            var cmdInsert =
                new SqlCommand("Insert Into CreditRisks" +
                $"(FirstName, LastName) Values('{fName}', '{lName}')",
                _sqlConnection);

            // We will get this from the connection object.
            SqlTransaction tx = null;
            try
            {
                tx = _sqlConnection.BeginTransaction();

                // Enlist the commands into this transaction.
                cmdInsert.Transaction = tx;
                cmdRemove.Transaction = tx;

                // Execute the commands.
                cmdInsert.ExecuteNonQuery();
                cmdRemove.ExecuteNonQuery();

                // Simulate error.
                if (throwEx)
                {
                    throw new Exception("Sorry!  Database error! Tx failed...");
                }

                // Commit it!
                tx.Commit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // Any error will roll back transaction.  Using the new conditional access operator to check for null.
                tx?.Rollback();
            }
        }
    }
}
