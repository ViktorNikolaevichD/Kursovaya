namespace Kursovaya.Entities
{
    public class Certificate
    {
        // Id медицинской справки
        public int Id { get; set; }
        // Id врача
        public int DoctorId { get; set; }
        // Навигационное свойство на таблицу Doctor
        public Doctor Doctor { get; set; }
        // Id пациента
        public int PatientId {  get; set; }
        // Навигационное свойство на таблицу Patient
        public Patient Patient { get; set; }
        // Id болезни
        public int DiseaseId { get; set; }
        // Навигационное свойство на таблицу Disease
        public Disease Disease { get; set; }
        // Состояние больничного (открыт, закрыт)
        public string Condition {  get; set; }
    }
}
