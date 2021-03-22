import { generateId } from "colyseus";

export interface IStoredUser {
    _id: string;
    sessionId?: string;
    token: string;
    userName: string;
}

class UserStorage {
    public readonly allUsers: Array<IStoredUser> = new Array();

    public findById(lookForId: string): IStoredUser {
        return this.allUsers.find(f => f._id === lookForId);
    }

    public findByToken(token: any): IStoredUser {
        return this.allUsers.find(f => f.token === token);
    }

    public newUser(data: { deviceId: string; platform: string; }): IStoredUser {
        const existingUser = this.allUsers.find(f => f.token === `${data.deviceId}${data.platform}`);
        if (existingUser) {
            return existingUser;
        }

        const newUser = {
            _id: generateId(),
            sessionId: null,
            token: `${data.deviceId}${data.platform}`,
            userName: 'user_name_any',
        };
        this.allUsers.push(newUser);
        return newUser;
    }
}

const storage = new UserStorage();

export { storage };