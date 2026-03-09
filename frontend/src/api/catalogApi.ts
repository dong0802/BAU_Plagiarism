import axiosClient from './axiosClient';

export interface FacultyDto {
    id: number;
    code: string;
    name: string;
    description?: string;
}

export interface DepartmentDto {
    id: number;
    code: string;
    name: string;
    facultyId: number;
    facultyName?: string;
}

export interface SubjectDto {
    id: number;
    code: string;
    name: string;
    credits: number;
    departmentId: number;
    departmentName?: string;
    facultyId?: number;
    facultyName?: string;
}

const catalogApi = {
    getFaculties: (): Promise<FacultyDto[]> => {
        return axiosClient.get('/catalog/faculties');
    },
    getDepartments: (facultyId?: number): Promise<DepartmentDto[]> => {
        return axiosClient.get('/catalog/departments', { params: { facultyId } });
    },
    getSubjects: (departmentId?: number, facultyId?: number): Promise<SubjectDto[]> => {
        return axiosClient.get('/catalog/subjects', { params: { departmentId, facultyId } });
    }
};

export default catalogApi;
