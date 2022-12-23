﻿using CalendrierCours.Entites;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace CalendrierCours.DAL.ExportCoursICS
{
    public class ExportCoursICS
        : IExportFichier
    {
        public void ExporterVersFichier(List<Cours> p_cours, string p_chemin)
        {
            if (p_cours is null)
            {
                throw new ArgumentNullException("Ne doit pas etre null", nameof(p_cours));
            }
            if (String.IsNullOrWhiteSpace(p_chemin))
            {
                throw new ArgumentNullException("Ne doit pas etre null ou vide", nameof(p_chemin));
            }

            List<CoursICSDTO> coursEport = p_cours.Select(c => new CoursICSDTO(c)).ToList();

            string fichier = this.RetournerNomFichier(p_chemin);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(this.EcrireEntete());

            coursEport.ForEach(c => c.Seances.ForEach(s => sb.AppendLine(this.EcrireSeance(c, s))));
            
            sb.AppendLine("END:VCALENDAR");

            this.EcrireFichier(fichier, sb.ToString());
        }

        private string RetournerNomFichier(string p_chemin)
        {
            bool estDisponible = true;
            string fichier = p_chemin + "\\cours.ics";

            do
            {
                int compteur = 1;
                if (!File.Exists(fichier))
                {
                    estDisponible = true;
                }
                else
                {
                    fichier = p_chemin + "\\cours" + compteur + ".ics";
                    compteur++;
                }

            } while (!estDisponible);

            return fichier;
        }

        private string EcrireEntete()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("BEGIN:VTIMEZONE");
            sb.AppendLine($"TZID:{this.AffecterParametreDepuisFichierConfig("TZID")}");
            sb.AppendLine("END:VTIMEZONE");

            return sb.ToString();
        }
        private string EcrireSeance(CoursICSDTO p_cours, SeanceICSDTO p_seance)
        {
            //DateTime dateDebut = p_seance.DateDebut.AddHours(5);
            //DateTime dateFin = p_seance.DateFin.AddHours(5);
            DateTime dateDebut = p_seance.DateDebut;
            DateTime dateFin = p_seance.DateFin;
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("BEGIN:VEVENT");

            if (p_cours.Categorie is not null)
            {
                sb.AppendLine($"CATEGORIES:{p_cours.Categorie}");
            }

            sb.Append($"DTSTART;TZID=\"{this.AffecterParametreDepuisFichierConfig("TZID")}\":");
            sb.Append($"{dateDebut.ToString("yyyyMMdd")}T");
            sb.AppendLine($"{dateDebut.ToString("HHmmss")}");
            sb.Append($"DTEND;TZID=\"{this.AffecterParametreDepuisFichierConfig("TZID")}\":");
            sb.Append($"{dateFin.ToString("yyyyMMdd")}T");
            sb.AppendLine($"{dateFin.ToString("HHmmss")}");
            sb.AppendLine($"LOCATION:{p_seance.Salle}");

            sb.Append("DESCRIPTION:");
            sb.Append($"Cours donné par {p_cours.Enseignant.Prenom} {p_cours.Enseignant.Nom}");
            if (p_cours.Description is not null)
            {
                sb.AppendLine(p_cours.Description);
            }
            else
            {
                sb.AppendLine();
            }

            sb.AppendLine($"SUMMARY;LANGUAGE={this.AffecterParametreDepuisFichierConfig("LANGUAGE")}:{p_cours.Numero} - {p_cours.Intitule}");
            sb.AppendLine($"UID:{p_seance.UID.ToString()}");
            sb.AppendLine($"END:VEVENT");

            return sb.ToString();
        }
        private void EcrireFichier(string p_fichier, string p_contenu)
        {
            FileStream fs = new FileStream(p_fichier, FileMode.Create);
            fs.Dispose();

            using (StreamWriter sw = new StreamWriter(p_fichier))
            {
                try
                {
                    sw.Write(p_contenu);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        private IConfigurationRoot LireFichierConfig()
        {
            IConfigurationRoot? configuration;

            try
            {
                configuration =
                    new ConfigurationBuilder()
                      .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                      .AddJsonFile("appsettings.json", false)
                      .Build();
            }
            catch (Exception e)
            {
                throw new InvalidDepotException("Le fichier de configuration est corrompu", e);
            }

            return configuration;
        }
        private string AffecterParametreDepuisFichierConfig(string p_nomParametre)
        {
            string? retour;
            IConfigurationRoot configuration = this.LireFichierConfig();

            if (configuration is null)
            {
                throw new Exception("Erreur dans la lecture du fichier de configuration");
            }

            retour = configuration[p_nomParametre];

            if (retour is null)
            {
                throw new Exception("Erreur dans la lecture du fichier de configuration");
            }

            return retour;
        }
    }
}